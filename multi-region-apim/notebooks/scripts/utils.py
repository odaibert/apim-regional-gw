import logging
import json
from azure.identity import DefaultAzureCredential
from azure.mgmt.apimanagement import ApiManagementClient
from azure.core.exceptions import ResourceNotFoundError

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

def run_az_command(command, subscription_id=None):
    try:
        import subprocess
        result = subprocess.run(command.split(), capture_output=True, text=True)
        if result.returncode != 0:
            logger.error(f"Command failed: {result.stderr}")
            return None
        return json.loads(result.stdout)
    except Exception as e:
        logger.error(f"Error running command: {str(e)}")
        return None

def verify_apim_deployment(subscription_id, resource_group, apim_name):
    try:
        credential = DefaultAzureCredential()
        client = ApiManagementClient(credential, subscription_id)
        apim = client.api_management_service.get(resource_group, apim_name)
        logger.info(f"APIM service {apim_name} found in {apim.location}")
        for location in apim.additional_locations:
            logger.info(f"Additional location: {location.location}")
        return True
    except ResourceNotFoundError:
        logger.error(f"APIM service {apim_name} not found")
        return False
    except Exception as e:
        logger.error(f"Error verifying APIM deployment: {str(e)}")
        return False
