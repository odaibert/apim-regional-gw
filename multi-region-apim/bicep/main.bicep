param location string = resourceGroup().location
param apimName string = 'apim-${uniqueString(resourceGroup().id)}'
param publisherName string = 'Contoso'
param publisherEmail string = 'admin@contoso.com'
param sku string = 'Premium'
param skuCount int = 1
param secondaryLocation string = 'westeurope'

resource apimService 'Microsoft.ApiManagement/service@2023-03-01-preview' = {
  name: apimName
  location: location
  sku: {
    name: sku
    capacity: skuCount
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
    additionalLocations: [
      {
        location: secondaryLocation
        sku: {
          name: sku
          capacity: skuCount
        }
      }
    ]
  }
}

output apimName string = apimService.name
output primaryGatewayUrl string = apimService.properties.gatewayUrl
output secondaryGatewayUrl string = '${apimService.properties.gatewayUrl}'.replace(location, secondaryLocation)
