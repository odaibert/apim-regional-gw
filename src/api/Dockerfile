FROM python:3.12-slim

WORKDIR /app
COPY requirements.txt .
RUN pip install -r requirements.txt

COPY main.py .

ENV AZURE_REGION=unknown

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "80"]
