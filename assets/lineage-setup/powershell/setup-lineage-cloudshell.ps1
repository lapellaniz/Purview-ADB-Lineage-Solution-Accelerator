# Setup Lineage for Synapse Spark Pools
# This can be run from Azure Cloud Shell

param (
    
    [String] $resourceGroupName,
    [String] $synapseWorkspaceName,
    [String] $synapseResourceGroupName


)

echo "This utility provisions Azure Resources such as Azure Function App and Event Hub to populate Purview with Synapse Spark Pool lineage."
echo "It also creates additional resources such as managed private endpoints, spark config and spark pool in Synapse Workspace."


if ([string]::IsNullOrEmpty($resourceGroupName)) {
    $resourceGroupName = Read-Host "Resource group for Azure Function App"
} 


if ([string]::IsNullOrEmpty($synapseWorkspaceName)) {
    $synapseWorkspaceName = Read-Host "Synapse Workspace Name"
} 

if ([string]::IsNullOrEmpty($synapseResourceGroupName)) {
    $synapseResourceGroupName = Read-Host "Synapse Workspace Resource Group Name"
} 


if([System.Convert]::ToBoolean($(az group exists -g $resourceGroupName))) {
     echo "Resource Group $resourceGroupName already exists."
}
else {
     
     echo "Please create resource group. az group create -g my_resoucegroup -l my_location"
     exit(0)
}


if((az resource list -g $resourceGroupName | ConvertFrom-Json).Count) {
     echo "Resource Group is not empty. Please create new resource group. Skipping resource provisioning."
}
else {
     
     echo "Deploying lineage accelerator ARM template..."
     $template_path = (Split-Path $PSScriptRoot) + "\purview-lineage-accelerator-template.json"
     az deployment group create --resource-group $resourceGroupName --template-file $template_path

}

$functionname=((az resource list -g $resourceGroupName | ConvertFrom-Json) | Where type -eq "Microsoft.Web/sites").name
$pename="$functionname-pe1"
$subscriptionId=(az account show | ConvertFrom-Json).id
echo $functionname $pename $subscriptionId

# Configure Synapse workspace with Managed Private Endpoints and Approve Private Endpoints
echo "Creating Private Endpoint to Azure Function App for lineage"
$jsonPEProperty = '{""privateLinkResourceId"": ""/subscriptions/' + ${subscriptionId} + '/resourceGroups/' + ${resourceGroupName} + '/providers/Microsoft.Web/sites/' + ${functionname} + '"", ""groupId"": ""sites""}'
az synapse managed-private-endpoints create --workspace-name $synapseWorkspaceName --pe-name $pename --file $jsonPEProperty 

ForEach($item in (az network private-endpoint-connection list --id "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Web/sites/$functionname" | ConvertFrom-Json) )
{
    if($item.properties.privateLinkServiceConnectionState.status -eq "Pending") {
        # Retrieve private endpoint connection id
        $peconnectionId=""
        while([string]::IsNullOrEmpty($peconnectionId))
        {
            $privateLinkServiceConnectionState=
            # Select the first connection in pending state and loop through all connections in pending state and approve each connection.
            $peconnectionId=((az network private-endpoint-connection list --id "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Web/sites/$functionname" | ConvertFrom-Json) | Where-Object { $_.properties.privateLinkServiceConnectionState.status -eq "Pending" } | Select -First 1).id
            echo "Waiting for private endpoint to be created..." 
            Start-Sleep -Seconds 5
        }
        
        # Approve Private Link
        echo "Approving link $peconnectionId. Please wait. This takes a few minutes."
        az network private-endpoint-connection approve  --id $peconnectionId --description "Approved using CLI"
    }
}



$functioncode=(az functionapp function keys list --function-name OpenLineageIn --name $functionname --resource-group $resourceGroupName | ConvertFrom-Json).default

# Create Spark Config
echo "Creating Spark Config..."
$conFilePath = ($PSScriptRoot) + "\synapse-sparkpool.conf"

New-Item $conFilePath -ItemType File -Force
Add-Content -Path $conFilePath -Value "spark.jars.packages io.openlineage:openlineage-spark:0.13.0" -PassThru
Add-Content -Path $conFilePath -Value "spark.extraListeners io.openlineage.spark.agent.OpenLineageSparkListener" -PassThru
Add-Content -Path $conFilePath -Value "spark.openlineage.host  https://$functionname.azurewebsites.net" -PassThru
Add-Content -Path $conFilePath -Value "spark.openlineage.namespace $synapseWorkspaceName,azuresynapsespark" -PassThru
Add-Content -Path $conFilePath -Value "spark.openlineage.url.param.code   $functioncode" -PassThru

# Create Spark Pool with spark config
echo "Creating spark pool.."
az synapse spark pool create --name "testlineagepool" --workspace-name $synapseWorkspaceName --resource-group $synapseResourceGroupName `
--spark-version 3.2 --node-count 3 --node-size Small --spark-config-file-path $conFilePath