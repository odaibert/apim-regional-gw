#!/bin/bash

# Configuration for multi-region APIM deployment
RESOURCE_GROUP="apim-geo-demo-rg"
LOCATION_PRIMARY="eastus"
LOCATION_SECONDARY="brazilsouth"
APIM_NAME="apim-geo-demo"
TM_PROFILE_NAME="apim-geo-demo-tm"
ACR_NAME="apimgeodemoreg"

az group create --name $RESOURCE_GROUP --location $LOCATION_PRIMARY

# Create ACR and build image
az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Basic --admin-enabled true
az acr build --registry $ACR_NAME --image apim-demo:latest ./api

# Create APIM with Premium SKU (required for multi-region)
az apim create \
    --name $APIM_NAME \
    --resource-group $RESOURCE_GROUP \
    --publisher-name "Geographic Routing Demo" \
    --publisher-email "admin@example.com" \
    --location $LOCATION_PRIMARY \
    --sku-name Premium_1 \
    --no-wait

# Create Traffic Manager profile
az network traffic-manager profile create \
    --name $TM_PROFILE_NAME \
    --resource-group $RESOURCE_GROUP \
    --routing-method Geographic \
    --unique-dns-name $TM_PROFILE_NAME \
    --monitor-protocol HTTPS \
    --monitor-port 443 \
    --monitor-path "/health"

# Wait for primary APIM to be ready
while [[ "$(az apim show --name $APIM_NAME --resource-group $RESOURCE_GROUP --query 'provisioningState' -o tsv 2>/dev/null)" != "Succeeded" ]]; do
    echo "APIM still provisioning... checking again in 5 minutes"
    sleep 300
done

# Add secondary region
az apim update \
    --name $APIM_NAME \
    --resource-group $RESOURCE_GROUP \
    --add-region $LOCATION_SECONDARY

# Deploy backend API to both regions
for location in $LOCATION_PRIMARY $LOCATION_SECONDARY; do
    az appservice plan create \
        --name "apim-demo-plan-$location" \
        --resource-group $RESOURCE_GROUP \
        --location $location \
        --sku P1V2

    az webapp create \
        --name "apim-demo-$location" \
        --resource-group $RESOURCE_GROUP \
        --plan "apim-demo-plan-$location" \
        --deployment-container-image-name "$ACR_NAME.azurecr.io/apim-demo:latest"

    az webapp config appsettings set \
        --name "apim-demo-$location" \
        --resource-group $RESOURCE_GROUP \
        --settings AZURE_REGION=$location
done

# Configure Traffic Manager endpoints
PRIMARY_ID=$(az apim show --name $APIM_NAME --resource-group $RESOURCE_GROUP --query 'id' -o tsv)

az network traffic-manager endpoint create \
    --name primary \
    --profile-name $TM_PROFILE_NAME \
    --resource-group $RESOURCE_GROUP \
    --type azureEndpoints \
    --target-resource-id $PRIMARY_ID \
    --geo-mapping GEO-NA \
    --endpoint-status Enabled

az network traffic-manager endpoint create \
    --name secondary \
    --profile-name $TM_PROFILE_NAME \
    --resource-group $RESOURCE_GROUP \
    --type azureEndpoints \
    --target-resource-id $PRIMARY_ID \
    --geo-mapping GEO-SA \
    --endpoint-status Enabled
