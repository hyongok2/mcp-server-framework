## 🧪 Micube MCP Server Framework - Test Suite

이 문서는 Micube MCP Server Framework의 테스트 구조와 실행 방법에 대해 설명합니다.

### 📁 테스트 구조

```
tests/
├── Micube.MCP.SDK.Tests/           # SDK 컴포넌트 테스트
│   ├── Abstracts/
│   │   └── BaseToolGroupTests.cs
│   └── Models/
│       └── ToolCallResultTests.cs
├── Micube.MCP.Core.Tests/          # Core 컴포넌트 테스트
│   ├── Dispatchers/
│   ├── Handlers/
│   ├── Services/
│   ├── Validation/
│   ├── Loader/
│   └── TestHelpers/
└── Micube.MCP.Server.Tests/        # Server 컴포넌트 테스트
    ├── Controllers/
    └── Integration/
```

### 🛠️ 사용된 테스트 도구

- **xUnit**: 기본 테스트 프레임워크
- **FluentAssertions**: 가독성 좋은 assertion 라이브러리
- **Moq**: 모킹 프레임워크
- **Microsoft.AspNetCore.Mvc.Testing**: ASP.NET Core 통합 테스트

### 🚀 테스트 실행 방법

#### 1. 개별 프로젝트 테스트

```bash
# SDK 테스트만 실행
dotnet test tests/Micube.MCP.SDK.Tests

# Core 테스트만 실행  
dotnet test tests/Micube.MCP.Core.Tests

# Server 테스트만 실행
dotnet test tests/Micube.MCP.Server.Tests
```

#### 2. 특정 테스트 필터링

```bash
# 특정 테스트 클래스만 실행
dotnet test --filter "ClassName=MessageValidatorTests"

# 특정 테스트 메서드만 실행
dotnet test --filter "TestMethodName=HandleAsync_WithValidMessage_ReturnsSuccess"

# PowerShell 스크립트로 필터링
.\scripts\run-tests.ps1 -Filter "ClassName=MessageValidatorTests"
```

### 🧩 테스트 헬퍼 클래스

#### MockLogger
```csharp
// 테스트용 로거 - 로그 메시지 캡처 및 검증
var logger = new MockLogger();
// ... 테스트 실행
logger.InfoMessages.Should().Contain("Expected message");
```

#### TestDataBuilder
```csharp
// 테스트 데이터 생성 헬퍼
var message = TestDataBuilder.CreateMessage("initialize", "test-id");
var clientParams = TestDataBuilder.CreateClientInitializeParams();
```

#### TestToolGroup
```csharp
// 테스트용 Tool Group 구현
var toolGroup = new TestToolGroup(logger);
```

### 🔍 주요 테스트 시나리오

#### 1. 메시지 디스패처 테스트
- 유효한 메시지 처리
- 에러 메시지 처리
- 핸들러 라우팅
- 예외 처리

#### 2. 핸들러 테스트
- Initialize 핸들러
- Tools 관련 핸들러
- Resources 관련 핸들러
- Prompts 관련 핸들러

#### 3. 서비스 테스트
- CapabilitiesService
- ResourceService
- PromptService
- ToolQueryService

#### 4. SDK 테스트
- BaseToolGroup 동작
- ToolCallResult 생성
- 다양한 반환 타입 처리

#### 5. 통합 테스트
- HTTP 엔드포인트
- 헬스 체크
- 전체 시나리오

### 🐛 테스트 디버깅

#### Visual Studio / VS Code
1. 테스트 탐색기에서 개별 테스트 디버그 실행
2. 브레이크포인트 설정 후 디버그 모드로 실행

#### 커맨드라인
```bash
# 상세 로그와 함께 테스트 실행
dotnet test --verbosity detailed

# 실패한 테스트만 재실행
dotnet test --filter "TestCategory=Failed"
```

### 📝 테스트 작성 가이드라인

#### 1. 네이밍 컨벤션
```csharp
[Fact]
public async Task MethodName_WithCondition_ExpectedResult()
{
    // Arrange
    // Act  
    // Assert
}
```

#### 2. AAA 패턴 사용
```csharp
[Fact]
public void Test_Example()
{
    // Arrange - 테스트 데이터 준비
    var service = new TestService();
    
    // Act - 실제 동작 실행
    var result = service.DoSomething();
    
    // Assert - 결과 검증
    result.Should().Be("expected");
}
```

#### 3. FluentAssertions 사용
```csharp
// 좋은 예
result.Should().NotBeNull();
result.Items.Should().HaveCount(3);
result.Status.Should().Be(Status.Success);

// 피해야 할 예
Assert.NotNull(result);
Assert.Equal(3, result.Items.Count);
```

### 🔧 문제 해결

#### 테스트 실행 실패
1. 의존성 복원: `dotnet restore`
2. 빌드 확인: `dotnet build`
3. 캐시 정리: `dotnet clean`

#### 커버리지 리포트 생성 실패
1. ReportGenerator 설치 확인
2. 커버리지 파일 존재 확인
3. 권한 문제 확인

### 📚 추가 자료

- [xUnit 문서](https://xunit.net/)
- [FluentAssertions 문서](https://fluentassertions.com/)
- [Moq 문서](https://github.com/moq/moq4)
- [ASP.NET Core 테스트 문서](https://docs.microsoft.com/en-us/aspnet/core/test/)

---