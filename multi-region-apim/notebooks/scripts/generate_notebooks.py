import os
import nbformat as nbf
import json

# Create deploy notebook
nb = nbf.v4.new_notebook()
nb['cells'] = [
    nbf.v4.new_markdown_cell('# Multi-Region API Management Deployment\n\nThis notebook deploys an Azure API Management service across two regions using Premium SKU.'),
    nbf.v4.new_code_cell('import os\nimport sys\nimport json\nimport logging\nfrom datetime import datetime\nsys.path.append(os.path.dirname(__file__) + "/scripts")\nimport utils\n\nlogging.basicConfig(\n    level=logging.INFO,\n    format="%(asctime)s - %(levelname)s - %(message)s"\n)\nlogger = logging.getLogger(__name__)'),
    nbf.v4.new_markdown_cell('## Initialize Variables'),
    nbf.v4.new_code_cell('subscription_id = os.getenv("AZURE_SUBSCRIPTION_ID")\nresource_group_name = "rg-multi-region-apim"\nprimary_location = "eastus"\nsecondary_location = "westeurope"\napim_name = f"apim-{os.getenv("AZURE_SUBSCRIPTION_ID", "")[:8]}"\napim_sku = "Premium"'),
    nbf.v4.new_markdown_cell('## Verify Azure CLI Authentication'),
    nbf.v4.new_code_cell('def verify_azure_cli():\n    logger.info("Verifying Azure CLI authentication...")\n    result = utils.run_az_command("az account show")\n    if not result:\n        raise Exception("Azure CLI authentication failed")\n    logger.info(f"Authenticated as: {result.get("user", {}).get("name")}")\n    return result\n\nverify_azure_cli()'),
    nbf.v4.new_markdown_cell('## Deploy Bicep Template'),
    nbf.v4.new_code_cell('def deploy_bicep():\n    logger.info("Deploying Bicep template...")\n    deployment_name = f"apim-deploy-{datetime.now().strftime("%Y%m%d-%H%M%S")}"\n    command = f"az deployment group create --name {deployment_name} --resource-group {resource_group_name} --template-file ../bicep/main.bicep --parameters location={primary_location} secondaryLocation={secondary_location} apimName={apim_name}"\n    result = utils.run_az_command(command)\n    if not result:\n        raise Exception("Bicep deployment failed")\n    return result\n\ndeploy_bicep()'),
    nbf.v4.new_markdown_cell('## Get Deployment Outputs'),
    nbf.v4.new_code_cell('def get_deployment_outputs():\n    logger.info("Getting deployment outputs...")\n    command = f"az apim show -g {resource_group_name} -n {apim_name}"\n    result = utils.run_az_command(command)\n    if result:\n        logger.info(f"Primary Gateway URL: {result.get("gatewayUrl")}")\n        logger.info(f"Additional Locations: {result.get("additionalLocations", [])}")\n        logger.info(f"Mock Backend API URL: {result.get("gatewayUrl")}/mock")\n    return result\n\nget_deployment_outputs()')
]

# Save deploy notebook
with open('../deploy-multi-region-apim.ipynb', 'w') as f:
    nbf.write(nb, f)

# Create cleanup notebook
nb = nbf.v4.new_notebook()
nb['cells'] = [
    nbf.v4.new_markdown_cell('# Cleanup Resources\n\nThis notebook cleans up the deployed Azure resources.'),
    nbf.v4.new_code_cell('import os\nimport sys\nimport logging\nfrom datetime import datetime\nsys.path.append(os.path.dirname(__file__) + "/scripts")\nimport utils\n\nlogging.basicConfig(\n    level=logging.INFO,\n    format="%(asctime)s - %(levelname)s - %(message)s"\n)\nlogger = logging.getLogger(__name__)'),
    nbf.v4.new_markdown_cell('## Initialize Variables'),
    nbf.v4.new_code_cell('subscription_id = os.getenv("AZURE_SUBSCRIPTION_ID")\nresource_group_name = "rg-multi-region-apim"'),
    nbf.v4.new_markdown_cell('## Cleanup Resources'),
    nbf.v4.new_code_cell('def cleanup_resources():\n    logger.info(f"Cleaning up resource group: {resource_group_name}")\n    command = f"az group delete --name {resource_group_name} --yes --no-wait"\n    result = utils.run_az_command(command)\n    if result is not None:\n        logger.info("Cleanup initiated successfully")\n    else:\n        logger.error("Failed to initiate cleanup")\n    return result\n\ncleanup_resources()')
]

# Save cleanup notebook
with open('../cleanup-resources.ipynb', 'w') as f:
    nbf.write(nb, f)
