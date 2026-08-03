FROM python:3.11-slim

WORKDIR /evaluation

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY datasets ./datasets
COPY scripts ./scripts

CMD ["python", "scripts/run_ragas_evaluation.py"]
