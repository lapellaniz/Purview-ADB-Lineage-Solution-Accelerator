#! /bin/bash


# This needs to run from within network that is connected to Synapse Managed Vnet


echo "This utility creates type definitions and entities required for Synapse lineage."

tenantId=$1
clientId=$2
clientSecret=$3
purviewname=$4

scope="https%3A%2F%2Fpurview.azure.net%2F.default" #do not modify this

if [ -z "${tenantId}" ]; then
    echo "Azure Active Directory Tenant Id:"
    read tenantId
fi

if [ -z "${clientId}" ]; then
    echo "Service Principal Client Id:"
    read clientId
fi

if [ -z "${clientSecret}" ]; then
    echo "Service Principal Client Secret:"
    read clientSecret
fi

if [ -z "${purviewname}" ]; then
    echo "Purview Account Name:"
    read purviewname
fi

# Acquire AAD token for the purview resource
authorization=$(curl -X POST -H "Content-Type: application/x-www-form-urlencoded" --data "grant_type=client_credentials&client_id=$clientId&client_secret=$clientSecret&scope=$scope" "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token")
accesstoken=`jq -r '.access_token' <<< "$authorization"`


# Create typedef for purview_custom_connector_generic_entity_with_columns
curl --location -w "\n\nhttp status code=%{http_code}\n" --request POST \
-d @synapse-purview-types/purview_custom_connector_generic_entity_with_columns.json \
"https://$purviewname.purview.azure.com/catalog/api/atlas/v2/types/typedefs" \
--header 'Content-Type: application/json' \
--header "Authorization: Bearer $accesstoken" 

echo -e "\n"

# Create typedef for Azure Synapse Notebook
curl --location -w "\n\nhttp status code=%{http_code}\n" --request POST \
-d @synapse-purview-types/purview_synapse_notebook_typedef.json \
"https://$purviewname.purview.azure.com/catalog/api/atlas/v2/types/typedefs" \
--header 'Content-Type: application/json' \
--header "Authorization: Bearer $accesstoken" 

echo -e "\n"


# Create relationship between Synapse Workspace and Synapse Notebook
curl --location -w "\n\nhttp status code=%{http_code}\n" --request POST \
-d @synapse-purview-types/purview_synapse_workspace_notebook_relationship.json \
"https://$purviewname.purview.azure.com/catalog/api/atlas/v2/types/typedefs" \
--header 'Content-Type: application/json' \
--header "Authorization: Bearer $accesstoken" 

echo -e "\n"

# Create relationship between Synapse Notebook and Process typedef
curl --location -w "\n\nhttp status code=%{http_code}\n" --request POST \
-d @synapse-purview-types/purview_synapse_notebook_process_basic_relationship.json \
"https://$purviewname.purview.azure.com/catalog/api/atlas/v2/types/typedefs" \
--header 'Content-Type: application/json' \
--header "Authorization: Bearer $accesstoken" 

echo -e "\n"




