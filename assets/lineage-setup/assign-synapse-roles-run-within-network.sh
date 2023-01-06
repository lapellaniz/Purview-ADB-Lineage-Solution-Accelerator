#! /bin/bash


# This needs to run from within network that is connected to Synapse Managed Vnet

echo "This utility assigns synapse role definitions so that the lineage API can retrieve additional details on Spark jobs to populate Purview."

tenantId=$1
clientId=$2
synapseworkspacename=$3
resource="https://dev.azuresynapse.net/"

if [ -z "${tenantId}" ]; then
    echo "Azure Active Directory Tenant Id:"
    read tenantId
fi

if [ -z "${clientId}" ]; then
    echo "Service Principal Client Id:"
    read clientId
fi

if [ -z "${synapseworkspacename}" ]; then
    echo "Synapse Workspace Name:"
    read synapseworkspacename
fi

# Acqiure Access token using az cli. This does not require client secret.
token=$(az account get-access-token --resource $resource --tenant $tenantId | jq -r .accessToken)
objectid=$(az ad sp show --id $clientId | jq -r .id)
roleassignmentId=$(uuidgen)

# Assign synapse monitoring operator role definition to service principal

echo "Assigning synapse monitoring operator role definition to service principal..."
curl --location -w "\n\nhttp status code=%{http_code}\n" --request PUT \
"https://$synapseworkspacename.dev.azuresynapse.net/roleAssignments/$roleassignmentId?api-version=2020-12-01" \
--header 'Content-Type: application/json' \
--header "Authorization: Bearer $token" \
--data-raw '{
  "roleId": "8f9b2195-5b12-4a7c-af30-8f1f46197650",
  "principalId": "'$objectid'",
  "scope": "'workspaces/$synapseworkspacename'",
  "principalType": "ServicePrincipal"
}'


roleassignmentId=$(uuidgen)
# assign synapse apache spark administrator role definition to service principal
echo "Assigning synapse apache spark administrator role definition to service principal..."
curl --location -w "\n\nhttp status code=%{http_code}\n" --request PUT \
"https://$synapseworkspacename.dev.azuresynapse.net/roleAssignments/$roleassignmentId?api-version=2020-12-01" \
--header 'Content-Type: application/json' \
--header "Authorization: Bearer $token" \
--data-raw '{
  "roleId": "c3a6d2f1-a26f-4810-9b0f-591308d5cbf1",
  "principalId": "'$objectid'",
  "scope": "'workspaces/$synapseworkspacename'",
  "principalType": "ServicePrincipal"
}'









