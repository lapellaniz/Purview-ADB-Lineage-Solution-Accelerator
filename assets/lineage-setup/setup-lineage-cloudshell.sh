#! /bin/bash


# This can be run from Azure Cloud Shell

echo "This utility provisions Azure Resources such as Azure Function App and Event Hub to populate Purview with Synapse Spark Pool lineage."
echo "It also creates additional resources such as managed private endpoints, spark config and spark pool in Synapse Workspace."


resourceGroupName=$1
synapseWorkspaceName=$2
synapseResourceGroupName=$3

if [ -z "${resourceGroupName}" ]; then
    echo "Resource group for Azure Function App:"
    read resourceGroupName
fi

if [ -z "${synapseWorkspaceName}" ]; then
    echo "Synapse Workspace Name:"
    read synapseWorkspaceName
fi

if [ -z "${synapseResourceGroupName}" ]; then
    echo "Synapse Workspace Resource Group Name:"
    read synapseResourceGroupName
fi



# Deploy azure resources for lineage accelerator

if [ $(az group exists -g $resourceGroupName) ]
then
    echo "Resource Group $resourceGroupName already exists."
else
    echo "Please create resource group. az group create -g my_resoucegroup -l my_location"
    exit 0
fi

if [ $(az resource list -g lineage-demo | jq -r '. | length') > 0 ]
then 
    echo "Resource Group is not empty. Please create new resource group. Skipping resource provisioning."
else
    echo "Deploying lineage accelerator ARM template..."
    az deployment group create --resource-group $resourceGroupName --template-file ./purview-lineage-accelerator-template.json
fi


functionname=$(az resource list -g $resourceGroupName | jq -r '.[] | select(.type == "Microsoft.Web/sites") | .name')
pename="$functionname-pe1"
subscriptionId=$(az account show | jq -r '.id')

# Configure Synapse workspace with Managed Private Endpoints and Approve Private Endpoints
echo "Creating Private Endpoint to Azure Function App for lineage"
az synapse managed-private-endpoints create --workspace-name $synapseWorkspaceName --pe-name $pename  \
    --file "{\"privateLinkResourceId\": \"/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Web/sites/$functionname\",\"groupId\": \"sites\"}"

# Retrieve private endpoint connection id
peconnectionId=""
while [ -z "$peconnectionId" ]
do
   peconnectionId=$(az network private-endpoint-connection list --id /subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Web/sites/$functionname \
 | jq -r '.[] | select(.properties.privateLinkServiceConnectionState.status == "Pending") | .id')
  echo "Waiting for private endpoint to be created..." 
  sleep 5s
done

# Approve Private Link
echo "Approving link $peconnectionId. Please wait. This takes a few minutes."
az network private-endpoint-connection approve  --id $peconnectionId --description "Approved using CLI"


functioncode=$(az functionapp function keys list --function-name OpenLineageIn --name $functionname --resource-group $resourceGroupName | jq -r '.default')

# Create Spark Config
echo "Creating Spark Config..."
cd "${0%/*}"

echo "" > ./synapse-sparkpool.conf
echo "spark.jars.packages io.openlineage:openlineage-spark:0.13.0" >> ./synapse-sparkpool.conf
echo "spark.extraListeners io.openlineage.spark.agent.OpenLineageSparkListener" >> ./synapse-sparkpool.conf
echo "spark.openlineage.host  https://$functionname.azurewebsites.net" >> ./synapse-sparkpool.conf
echo "spark.openlineage.namespace $synapseWorkspaceName,azuresynapsespark" >> ./synapse-sparkpool.conf
echo "spark.openlineage.url.param.code    $functioncode" >> ./synapse-sparkpool.conf


# Create Spark Pool with spark config
echo "Creating spark pool.."
az synapse spark pool create --name "testlineagepool" --workspace-name $synapseWorkspaceName --resource-group $synapseResourceGroupName \
--spark-version 3.2 --node-count 3 --node-size Small --spark-config-file-path './synapse-sparkpool.conf'