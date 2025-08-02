## ✅ 1단계: Oracle XE Docker Compose 파일 생성

### 📄 `docker-compose.yml`

```yaml
version: "3.8"

services:
  oracle-xe:
    image: gvenzl/oracle-xe:21-slim
    container_name: oracle-xe
    ports:
      - "1521:1521"     # Oracle listener
      - "8080:8080"     # APEX/HTTP (원할 시)
    environment:
      ORACLE_PASSWORD: oracle
    volumes:
      - ./oracle-data:/opt/oracle/oradata           # DB 파일 보존
      - ./tns:/opt/oracle/homes/OraDBHome21c/network/admin/  # tnsnames.ora 마운트 (선택사항)
    restart: unless-stopped
```

---

## ✅ 2단계: `tnsnames.ora` (선택 사항, TNS 사용 시)

### 📄 `tns/tnsnames.ora`

```ora
MYXE =
  (DESCRIPTION =
    (ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 1521))
    (CONNECT_DATA =
      (SERVICE_NAME = XEPDB1)
    )
  )
```

* `./tns` 폴더는 `docker-compose.yml`과 같은 경로에 있어야 합니다.
* TNS 방식 접속(`Data Source=MYXE`)을 원하지 않으면 생략해도 됩니다.

---

## ✅ 3단계: 실행

```bash
docker compose up -d
```

**이후 확인:**

```bash
docker logs -f oracle-xe
```

성공 메시지 예시:

```
DATABASE IS READY TO USE!
```

---

## ✅ 4단계: 접속 정보 정리

| 항목          | 값                        |
| ----------- | ------------------------ |
| 호스트         | `localhost`              |
| 포트          | `1521`                   |
| 사용자         | `system`                 |
| 비밀번호        | `oracle`                 |
| 기본 서비스명     | `XEPDB1`                 |
| TNS 이름 (선택) | `MYXE` (tnsnames.ora 기준) |

---

## ✅ 5단계: MCP ToolGroup 연결 문자열

* 직접:

  ```csharp
  Data Source=localhost:1521/XEPDB1
  ```

* TNS 방식:

  ```csharp
  Data Source=MYXE
  ```

(단, 두 번째는 `.ora` 경로 조건 충족 필요)

---


##  **ora** 파일 사용에 관한 사항

* `.ora` 파일을 사용하여 연결 설정을 하는경우 ora 파일도 dll 파일과 같은 경로에 배치할 수 있다.
* 현재 Sample로 ora 파일 사용 Case와 Connecting String만 사용하는 Case의 json을 제공하고 있다.