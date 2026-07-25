# Bu Dockerfile SADECE Linux/geliştirme için opsiyonel bir yol (bkz. CLAUDE.md § Mimari Kararlar) —
# üretimde mediapipe-service hedef makinede native process olarak çalışır. F5.01 spike'ında bu proje
# özelinde Fedora'nın araç zinciriyle mediapipe uyumsuzluğu bulundu; bu image standart bir Debian
# tabanında olduğu için o sorunu yaşamıyor (doğrulandı).
FROM python:3.10-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libgl1 libglib2.0-0 libxcb1 libxext6 libsm6 libgles2 libegl1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

EXPOSE 8000

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
