# 🔧 서버 설정

> **appsettings.json의 모든 설정 옵션을 완벽하게 마스터합니다**

MCP Server Framework는 `config/appsettings.json` 파일을 통해 모든 설정을 관리합니다. 이 문서에서는 각 설정 항목의 의미와 최적화 방법을 상세히 설명합니다.

## 📋 설정 파일 구조

### **기본 설정 템플릿**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "None",
      "Microsoft.Hosting.Lifetime": "None"
    },
    "MinLevel": "Info",
    "File": {
      "Directory": "C:\\Logs\\MCPServer",
      "FlushIntervalSeconds": 2,
      "MaxFileSizeMB": 50,
      "RetentionDays": 30
    }
  },
  "ToolGroups": {
    "Directory": "tools",
    "Whitelist": ["SampleTools.dll"]
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  },
  "Features": {
    "EnableStdio": true,
    "EnableHttp": true
  },
  "Resources": {
    "Directory": "docs",
    "MetadataFileName": ".mcp-resources.json",
    "SupportedExtensions": [".md", ".txt", ".json", ".yaml", ".yml", ".xml"]
  },
  "Prompts": {
    "Directory": "prompts"
  }
}
```

## 🚀 핵심 설정 섹션

### **1. Features - 기능 활성화**

```json
{
  "Features": {
    "EnableStdio": true,    // STDIO 전송 활성화
    "EnableHttp": true      // HTTP 전송 활성화
  }
}
```

#### **설정 옵션**
| 옵션 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `EnableStdio` | bool | `true` | 표준 입출력 기반 통신 활성화 |
| `EnableHttp` | bool | `true` | HTTP API 기반 통신 활성화 |

#### **사용 시나리오**
```json
// 개발 환경 - 모든 방식 활성화
{ "EnableStdio": true, "EnableHttp": true }

// 프로덕션 - STDIO만 사용
{ "EnableStdio": true, "EnableHttp": false }

// 웹 서비스 - HTTP만 사용  
{ "EnableStdio": false, "EnableHttp": true }
```

### **2. Kestrel - 웹 서버 설정**

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "password"
        }
      }
    },
    "Limits": {
      "MaxRequestBodySize": 52428800,    // 50MB
      "RequestHeadersTimeout": "00:00:30"
    }
  }
}
```

#### **주요 설정**
- **`Url`**: 서버 바인딩 주소 (`0.0.0.0`은 모든 인터페이스)
- **`Certificate`**: HTTPS 인증서 설정
- **`MaxRequestBodySize`**: 최대 요청 크기
- **`RequestHeadersTimeout`**: 헤더 타임아웃

#### **환경별 설정 예시**
```json
// 로컬 개발
"Url": "http://localhost:5000"

// Docker 컨테이너
"Url": "http://0.0.0.0:5000" 

```

## 🛠️ ToolGroups - 도구 설정

```json
{
  "ToolGroups": {
    "Directory": "tools",
    "Whitelist": [
      "SampleTools.dll",
      "ProductionTools.dll",
      "CustomTools.dll"
    ]
  }
}
```

### **설정 옵션**
| 옵션 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `Directory` | string | `"tools"` | 도구 DLL이 위치한 디렉토리 |
| `Whitelist` | string[] | `[]` | 로드할 DLL 파일명 목록 |

### **보안 정책**
```json
{
  "ToolGroups": {
    // ✅ 좋은 예: 명시적 화이트리스트
    "Whitelist": [
      "ApprovedTool.dll",
      "VerifiedTool.dll"
    ]
  }
}

```

### **화이트리스트 패턴**
```json
// 개발 환경 - 모든 도구 허용
"Whitelist": ["*.dll"]

// 테스트 환경 - 테스트 도구만
"Whitelist": ["TestTools.dll", "MockTools.dll"]

// 프로덕션 - 승인된 도구만
"Whitelist": ["ProductionTools.dll", "SecurityTools.dll"]
```

## 📄 Resources - 리소스 설정

```json
{
  "Resources": {
    "Directory": "docs",
    "MetadataFileName": ".mcp-resources.json",
    "SupportedExtensions": [
      ".md", ".txt", ".json", 
      ".yaml", ".yml", ".xml"
    ]
  }
}
```

### **설정 옵션**
| 옵션 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `Directory` | string | `"docs"` | 리소스 파일이 위치한 디렉토리 |
| `MetadataFileName` | string | `".mcp-resources.json"` | 메타데이터 파일명 |
| `SupportedExtensions` | string[] | `[".md", ".txt", ...]` | 지원하는 파일 확장자 |

### **고급 설정 예시**
```json
{
  "Resources": {
    "Directory": "documents",
    "MetadataFileName": "resource-info.json",
    "SupportedExtensions": [
      ".md", ".txt", ".json", ".yaml", 
      ".csv", ".xml", ".html", ".pdf"
    ],
    "MaxFileSize": 10485760,  // 10MB (향후 지원)
    "EnableCaching": true     // 캐싱 활성화 (향후 지원)
  }
}
```

## 💬 Prompts - 프롬프트 설정

```json
{
  "Prompts": {
    "Directory": "prompts"
  }
}
```

### **디렉토리 구조 예시**
```
prompts/
├── code-review.json          # 프롬프트 정의
├── code-review.md           # 템플릿 파일
├── documentation.json
├── documentation.md
└── templates/
    ├── summary-template.md
    └── report-template.md
```

### **고급 설정 (향후 확장)**
```json
{
  "Prompts": {
    "Directory": "prompts",
    "EnableTemplateCache": true,
    "DefaultRole": "user",
    "MaxTemplateSize": 1048576  // 1MB
  }
}
```

## 📝 Logging - 로깅 설정

### **기본 로깅 설정**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "None",
      "Microsoft.Hosting.Lifetime": "None"
    },
    "MinLevel": "Info",
    "File": {
      "Directory": "C:\\Logs\\MCPServer",
      "FlushIntervalSeconds": 2,
      "MaxFileSizeMB": 50,
      "RetentionDays": 30
    }
  }
}
```

### **로그 레벨 상세**
| 레벨 | 용도 | 권장 환경 |
|------|------|-----------|
| `Debug` | 상세한 디버깅 정보 | 개발 |
| `Info` | 일반적인 정보 | 테스트, 프로덕션 |
| `Error` | 오류만 기록 | 프로덕션 (최소 로깅) |

### **파일 로깅 설정**
| 옵션 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `Directory` | string | `"logs"` | 로그 파일 저장 디렉토리 |
| `FlushIntervalSeconds` | int | `2` | 로그 플러시 간격 (초) |
| `MaxFileSizeMB` | int | `50` | 로그 파일 최대 크기 |
| `RetentionDays` | int | `30` | 로그 파일 보존 기간 |

### **환경별 로깅 설정**
```json
// 개발 환경 - 상세 로깅
{
  "Logging": {
    "MinLevel": "Debug",
    "File": {
      "Directory": "./logs",
      "FlushIntervalSeconds": 1,
      "MaxFileSizeMB": 10,
      "RetentionDays": 7
    }
  }
}

// 프로덕션 - 최적화된 로깅
{
  "Logging": {
    "MinLevel": "Info", 
    "File": {
      "Directory": "/var/log/mcp-server",
      "FlushIntervalSeconds": 5,
      "MaxFileSizeMB": 100,
      "RetentionDays": 90
    }
  }
}
```

## 🎯 환경별 설정 최적화

### **Development (개발)**
```json
{
  "Logging": { "MinLevel": "Debug" },
  "Features": { "EnableStdio": true, "EnableHttp": true },
  "ToolGroups": { "Whitelist": ["*.dll"] },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5000" }
    }
  }
}
```

### **Production (프로덕션)**
```json
{
  "Logging": { 
    "MinLevel": "Info",
    "File": {
      "Directory": "/var/log/mcp-server",
      "RetentionDays": 90
    }
  },
  "Features": { "EnableStdio": true, "EnableHttp": false },
  "ToolGroups": { 
    "Whitelist": ["ProductionTools.dll", "SecurityTools.dll"] 
  },
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:443",
        "Certificate": {
          "Path": "/etc/ssl/mcp-server.pfx",
          "Password": "${CERT_PASSWORD}"
        }
      }
    }
  }
}
```

## 🔐 보안 설정

### **화이트리스트 보안**
```json
{
  "ToolGroups": {
    "Whitelist": [
      // ✅ 승인된 도구만 명시
      "ApprovedTool.dll",
      "SecurityAuditedTool.dll"
    ]
  }
}
```

### **네트워크 보안**
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5001"
      }
    },
    "Limits": {
      "MaxRequestBodySize": 1048576,  // 1MB 제한
      "MaxConcurrentConnections": 100
    }
  }
}
```

## 🚀 성능 튜닝

### **로깅 성능**
```json
{
  "Logging": {
    "File": {
      "FlushIntervalSeconds": 5,     // 더 긴 간격
      "MaxFileSizeMB": 100,          // 더 큰 파일
      "RetentionDays": 30            // 적절한 보존 기간
    }
  }
}
```

### **네트워크 성능**
```json
{
  "Kestrel": {
    "Limits": {
      "MaxRequestBodySize": 52428800,      // 50MB
      "KeepAliveTimeout": "00:05:00",      // 5분
      "RequestHeadersTimeout": "00:00:30"   // 30초
    }
  }
}
```

## 🧪 설정 검증

### **시작 시 자동 검증**
서버는 시작할 때 다음을 검증합니다:
- 필수 디렉토리 존재 확인
- 도구 DLL 파일 유효성
- 네트워크 포트 사용 가능성
- 로그 파일 쓰기 권한

### **검증 오류 예시**
```
❌ Configuration validation failed:
- Tools directory not found: /app/tools
- Invalid log retention days: -1
- Port 5000 already in use
- Missing write permission for log directory
```

## 💡 설정 백업

```bash
# 배포 전 설정 백업
cp config/appsettings.json backups/appsettings.$(date +%Y%m%d).json
```
---

**다음**: [클라이언트 연결](client-connection.md) - MCP 클라이언트 연결 방법 →

**이전**: [← Configuration 홈](README.md)