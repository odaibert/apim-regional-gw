import os
import nbformat as nbf
import json

# Create deploy notebook
nb = nbf.v4.new_notebook()
nb['cells'] = [
    nbf.v4.new_markdown_cell('# Multi-Region API Management Deployment\n\nThis notebook deploys an Azure API Management service across two regions using Premium SKU.'),
    nbf.v4.new_code_cell('import os\nimport sys\nimport json\nimport logging\nfrom datetime import datetime\n\nnotebook_dir = "/home/ubuntu/repos/TestDevin/multi-region-apim/notebooks"\nsys.path.append(os.path.join(notebook_dir, "scripts"))\nimport utils\n\nlogging.basicConfig(\n    level=logging.INFO,\n    format="%(asctime)s - %(levelname)s - %(message)s"\n)\nlogger = logging.getLogger(__name__)'),
    nbf.v4.new_markdown_cell('## Initialize Variables'),
    nbf.v4.new_markdown_cell('## Configure Environment Variables\n\nMake sure to set the following environment variables:\n- AZURE_SUBSCRIPTION_ID: Your Azure subscription ID\n- AZURE_RESOURCE_GROUP: Resource group name (default: rg-multi-region-apim)\n- AZURE_APIM_NAME: API Management instance name (default: generated from subscription)'),
    nbf.v4.new_code_cell('''subscription_id = os.getenv("AZURE_SUBSCRIPTION_ID")
if not subscription_id:
    raise ValueError("AZURE_SUBSCRIPTION_ID environment variable is required")

resource_group_name = os.getenv("AZURE_RESOURCE_GROUP", "rg-multi-region-apim")
apim_name = os.getenv("AZURE_APIM_NAME", f"apim-{subscription_id[:8]}")
primary_location = "eastus"
secondary_location = "westeurope"
apim_sku = "Premium"'''),
    nbf.v4.new_markdown_cell('## Verify Azure CLI Authentication'),
    nbf.v4.new_code_cell('def verify_azure_cli():\n    logger.info("Verifying Azure CLI authentication...")\n    result = utils.run_az_command("az account show")\n    if not result:\n        raise Exception("Azure CLI authentication failed")\n    logger.info(f"Authenticated as: {result.get("user", {}).get("name")}")\n    return result\n\nverify_azure_cli()'),
    nbf.v4.new_markdown_cell('## Create Resource Group'),
    nbf.v4.new_code_cell('def create_resource_group():\n    logger.info(f"Creating resource group: {resource_group_name} in {primary_location}")\n    command = f"az group create --name {resource_group_name} --location {primary_location}"\n    result = utils.run_az_command(command)\n    if not result:\n        raise Exception("Resource group creation failed")\n    logger.info("Resource group created successfully")\n    return result\n\ncreate_resource_group()'),
    nbf.v4.new_markdown_cell('## Validate and Deploy Bicep Template'),
    nbf.v4.new_code_cell('def deploy_bicep():\n    logger.info("Deploying Bicep template...")\n    deployment_name = f"apim-deploy-{datetime.now().strftime("%Y%m%d-%H%M%S")}"\n    \n    # First, validate the template\n    logger.info("Validating Bicep template...")\n    validate_command = f"az deployment group validate --name {deployment_name} --resource-group {resource_group_name} --template-file ../bicep/main.bicep --parameters location={primary_location} secondaryLocation={secondary_location} apimName={apim_name}"\n    validation_result = utils.run_az_command(validate_command)\n    if not validation_result:\n        raise Exception("Bicep template validation failed")\n    logger.info("Template validation successful")\n    \n    # Then deploy if validation passes\n    logger.info("Starting deployment...")\n    command = f"az deployment group create --name {deployment_name} --resource-group {resource_group_name} --template-file ../bicep/main.bicep --parameters location={primary_location} secondaryLocation={secondary_location} apimName={apim_name}"\n    result = utils.run_az_command(command)\n    if not result:\n        raise Exception("Bicep deployment failed")\n    return result\n\ndeploy_bicep()'),
    nbf.v4.new_markdown_cell('## Get Deployment Outputs'),
    nbf.v4.new_code_cell('def get_deployment_outputs():\n    logger.info("Getting deployment outputs...")\n    command = f"az apim show -g {resource_group_name} -n {apim_name}"\n    result = utils.run_az_command(command)\n    if result:\n        logger.info(f"Primary Gateway URL: {result.get("gatewayUrl")}")\n        logger.info(f"Additional Locations: {result.get("additionalLocations", [])}")\n        logger.info(f"Mock Backend API URL: {result.get("gatewayUrl")}/mock")\n    return result\n\nget_deployment_outputs()'),
    nbf.v4.new_markdown_cell('## Verify Traffic Manager Configuration'),
    nbf.v4.new_code_cell('''def verify_traffic_manager():
    logger.info("Verifying Traffic Manager deployment...")
    tm_name = f"{apim_name}-tm"
    
    # Check Traffic Manager profile
    command = f"az network traffic-manager profile show -g {resource_group_name} -n {tm_name}"
    result = utils.run_az_command(command)
    if not result:
        logger.error("Traffic Manager profile not found")
        raise Exception("Traffic Manager profile not found")
    
    logger.info(f"Traffic Manager URL: https://{result.get('dnsConfig', {}).get('relativeName')}.trafficmanager.net")
    logger.info(f"Routing method: {result.get('properties', {}).get('trafficRoutingMethod')}")
    
    # Check endpoint health
    endpoints_command = f"az network traffic-manager endpoint list -g {resource_group_name} --profile-name {tm_name}"
    endpoints = utils.run_az_command(endpoints_command)
    if not endpoints:
        logger.error("No endpoints found in Traffic Manager profile")
        raise Exception("No endpoints found in Traffic Manager profile")
    
    for endpoint in endpoints:
        endpoint_props = endpoint.get('properties', {})
        logger.info(f"Endpoint {endpoint.get('name')}:")
        logger.info(f"  - Status: {endpoint_props.get('endpointMonitorStatus', 'Unknown')}")
        logger.info(f"  - Target Resource: {endpoint_props.get('targetResourceId')}")
        logger.info(f"  - Location: {endpoint_props.get('endpointLocation')}")
        logger.info(f"  - Geo-mapping: {endpoint_props.get('geoMapping', [])}")
    
    return result

verify_traffic_manager()''')
]

# Save deploy notebook
with open('../deploy-multi-region-apim.ipynb', 'w') as f:
    nbf.write(nb, f)

# Create cleanup notebook
nb = nbf.v4.new_notebook()
nb['cells'] = [
    nbf.v4.new_markdown_cell('# Cleanup Resources\n\nThis notebook cleans up the deployed Azure resources.'),
    nbf.v4.new_code_cell('import os\nimport sys\nimport logging\nfrom datetime import datetime\n\nnotebook_dir = "/home/ubuntu/repos/TestDevin/multi-region-apim/notebooks"\nsys.path.append(os.path.join(notebook_dir, "scripts"))\nimport utils\n\nlogging.basicConfig(\n    level=logging.INFO,\n    format="%(asctime)s - %(levelname)s - %(message)s"\n)\nlogger = logging.getLogger(__name__)'),
    nbf.v4.new_markdown_cell('## Initialize Variables'),
    nbf.v4.new_code_cell('subscription_id = os.getenv("AZURE_SUBSCRIPTION_ID")\nresource_group_name = "rg-multi-region-apim"'),
    nbf.v4.new_markdown_cell('## Cleanup Resources'),
    nbf.v4.new_code_cell('def cleanup_resources():\n    logger.info(f"Cleaning up resource group: {resource_group_name}")\n    command = f"az group delete --name {resource_group_name} --yes --no-wait"\n    result = utils.run_az_command(command)\n    if result is not None:\n        logger.info("Cleanup initiated successfully")\n    else:\n        logger.error("Failed to initiate cleanup")\n    return result\n\ncleanup_resources()')
]

# Save cleanup notebook
with open('../cleanup-resources.ipynb', 'w') as f:
    nbf.write(nb, f)
