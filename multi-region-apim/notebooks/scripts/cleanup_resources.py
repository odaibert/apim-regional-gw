import os
import sys
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

def cleanup_resources():
    logger.info(f"Cleaning up resource group: {resource_group_name}")
    command = f'az group delete --name {resource_group_name} --yes --no-wait'
    result = utils.run_az_command(command)
    if result is not None:
        logger.info("Cleanup initiated successfully")
    else:
        logger.error("Failed to initiate cleanup")
    return result

def main():
    try:
        cleanup_resources()
    except Exception as e:
        logger.error(f"Cleanup failed: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()
