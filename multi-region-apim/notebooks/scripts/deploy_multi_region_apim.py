import os
import sys
import json
import logging
from datetime import datetime
sys.path.append(os.path.dirname(__file__))
import utils

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

subscription_id = os.getenv('AZURE_SUBSCRIPTION_ID')
resource_group_name = 'rg-multi-region-apim'
primary_location = 'eastus'
secondary_location = 'westeurope'
apim_name = f'apim-{os.getenv("AZURE_SUBSCRIPTION_ID", "")[:8]}'
apim_sku = 'Premium'

def verify_azure_cli():
    logger.info("Verifying Azure CLI authentication...")
    result = utils.run_az_command('az account show')
    if not result:
        raise Exception("Azure CLI authentication failed")
    logger.info(f"Authenticated as: {result.get('user', {}).get('name')}")
    return result

def deploy_bicep():
    logger.info("Deploying Bicep template...")
    deployment_name = f'apim-deploy-{datetime.now().strftime("%Y%m%d-%H%M%S")}'
    
    # First, validate the template
    logger.info("Validating Bicep template...")
    validate_command = f'az deployment group validate --name {deployment_name} --resource-group {resource_group_name} --template-file ../bicep/main.bicep --parameters location={primary_location} secondaryLocation={secondary_location} apimName={apim_name}'
    validation_result = utils.run_az_command(validate_command)
    if not validation_result:
        raise Exception("Bicep template validation failed")
    logger.info("Template validation successful")
    
    # Then deploy if validation passes
    logger.info("Starting deployment...")
    command = f'az deployment group create --name {deployment_name} --resource-group {resource_group_name} --template-file ../bicep/main.bicep --parameters location={primary_location} secondaryLocation={secondary_location} apimName={apim_name}'
    result = utils.run_az_command(command)
    if not result:
        raise Exception("Bicep deployment failed")
    return result

def get_deployment_outputs():
    logger.info("Getting deployment outputs...")
    command = f'az apim show -g {resource_group_name} -n {apim_name}'
    result = utils.run_az_command(command)
    if result:
        logger.info(f"Primary Gateway URL: {result.get('gatewayUrl')}")
        logger.info(f"Additional Locations: {result.get('additionalLocations', [])}")
        logger.info(f"Mock Backend API URL: {result.get('gatewayUrl')}/mock")
    return result

def verify_traffic_manager():
    logger.info("Verifying Traffic Manager deployment...")
    tm_name = f'{apim_name}-tm'
    command = f'az network traffic-manager profile show -g {resource_group_name} -n {tm_name}'
    result = utils.run_az_command(command)
    if result:
        logger.info(f"Traffic Manager URL: https://{result.get('dnsConfig', {}).get('relativeName')}.trafficmanager.net")
        
        # Check endpoint health
        endpoints_command = f'az network traffic-manager endpoint list -g {resource_group_name} --profile-name {tm_name}'
        endpoints = utils.run_az_command(endpoints_command)
        if endpoints:
            for endpoint in endpoints:
                logger.info(f"Endpoint {endpoint.get('name')}: {endpoint.get('properties', {}).get('endpointMonitorStatus', 'Unknown')}")
    return result

def main():
    try:
        verify_azure_cli()
        deploy_bicep()
        get_deployment_outputs()
        verify_traffic_manager()
    except Exception as e:
        logger.error(f"Deployment failed: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()
