from fastapi import FastAPI, Request
import os

app = FastAPI(title="Regional API Demo")

REGION = os.getenv("AZURE_REGION", "unknown")

@app.get("/")
async def root(request: Request):
    return {
        "message": f"Hello from {REGION}!",
        "region": REGION,
        "client_ip": request.client.host
    }

@app.get("/status-0123456789abcdef")
async def health_check():
    return {"status": "healthy", "region": REGION}
