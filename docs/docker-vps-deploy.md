# Docker VPS Deploy

## 1. Trigger branch

Workflow chỉ chạy khi push lên nhánh:

```text
release
```

## 2. Docker Hub

Tạo repo private trước:

```text
kienkieuhihi/edubridge-backend
```

## 3. Cái cần setup trong GitHub Secrets

Chỉ cần 6 secret cho CI/CD:

```text
DOCKERHUB_USERNAME
DOCKERHUB_TOKEN
VPS_HOST
VPS_PORT
VPS_USER
VPS_SSH_KEY
```

Theo cách bạn đang dùng:

```text
VPS_USER=root
VPS_PORT=22
```

Lưu ý:

- `VPS_SSH_KEY` là private SSH key, không phải password.
- `DOCKERHUB_TOKEN` là Docker Hub access token.

## 4. Runtime env để trên VPS

File env nằm tại:

```text
/root/edubridge/.env
```

Workflow sẽ:

- copy file mẫu `.env.production.example`
- nếu chưa có `.env` thì tạo từ file mẫu
- dừng workflow để bạn sửa `.env`
- từ các lần sau chỉ cập nhật `APP_IMAGE` rồi deploy

## 5. Nội dung `.env` tối thiểu trên VPS

```env
APP_IMAGE=docker.io/kienkieuhihi/edubridge-backend:latest
APP_PORT=8080

MSSQL_PID=Developer
MSSQL_SA_PASSWORD=CHANGE_ME_TO_A_STRONG_PASSWORD
DB_NAME=EduBridgeDB
ENABLE_MOCK_SEED=false

JWT_ISSUER=EduBridge
JWT_AUDIENCE=EduBridge.Mobile
JWT_KEY=CHANGE_ME_TO_A_LONG_RANDOM_SECRET
JWT_ACCESS_TOKEN_MINUTES=1440

ALLOWED_HOSTS=*
CORS_ALLOW_ANY_ORIGIN=true
CORS_ALLOWED_ORIGINS=
```

Cho lần test đầu:

```env
APP_PORT=8080
DB_NAME=EduBridgeDB
ENABLE_MOCK_SEED=false
ALLOWED_HOSTS=*
CORS_ALLOW_ANY_ORIGIN=true
```

## 6. Setup lần đầu trên VPS

### Cài Docker

VPS cần có:

- Docker Engine
- Docker Compose plugin

### Tạo thư mục deploy

```bash
mkdir -p /root/edubridge
```

### Sau lần workflow đầu copy file lên VPS

Nếu `.env` chưa có, workflow sẽ fail có chủ đích. Khi đó:

```bash
cd /root/edubridge
cp .env.production.example .env
nano .env
```

Sửa tối thiểu:

- `MSSQL_SA_PASSWORD`
- `JWT_KEY`
- `APP_IMAGE`

Sau đó rerun workflow hoặc push lại nhánh `release`.

## 7. Web backend vẫn chạy

Container app là full ASP.NET app, nên sau deploy bạn vào:

```text
http://YOUR_VPS_IP:8080/Login
```

## 8. DB không bị mất khi deploy

- SQL Server dùng volume `edubridge_sql_data`
- `db-init` chỉ tạo DB nếu chưa có
- migration SQL được track bằng `dbo.__SchemaMigrations`
- script đã chạy rồi sẽ không chạy lại

Nên:

- deploy lại không mất data cũ
- thêm field/table bằng migration mới không làm mất DB cũ

## 9. Lần đầu tạo DB có sẵn tài khoản nền

Script init gốc đã có:

```text
owner@edubridge.com
teacher@edubridge.com
parent@edubridge.com
```

Password demo:

```text
123456
```

Và có sẵn trung tâm mẫu để web backend login/test.
