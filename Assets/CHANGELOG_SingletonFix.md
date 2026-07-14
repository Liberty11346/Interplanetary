# 수정 사항 정리 (2025-11-28)

> **참고**: 이 문서는 2025-11-28 에 적용된 싱글톤 코어 로직 개선 (Domain Reload 문제 해결) 을 문서화합니다.
> 현재 branch 에는 추가 초기화 (GameSceneInitializer, ApplicationLifecycleManager) 가 적용되어 있습니다.
> 전체 아키텍처 개요는 루트 `README.md` 와 `Assets/README.md` 를 참조하세요.

## 1. `SingletonBase.cs` (싱글톤 코어 로직 개선)

**목적:** Unity 에디터의 Domain Reload 문제 해결 및 애플리케이션 종료 시 인스턴스 접근 안전성 확보.

### 1.1. `SingletonRegistry` 클래스 추가 (신규)
종료 상태를 전역적으로 관리하고, 게임 시작 시 초기화하는 클래스를 추가했습니다.

```csharp
// [New Code]
using UnityEngine;

namespace CommonLib
{
    /// <summary>
    /// 싱글톤 상태 관리를 위한 레지스트리 (Domain Reload 문제 해결용)
    /// </summary>
    public static class SingletonRegistry
    {
        public static bool IsQuitting { get; set; } = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            IsQuitting = false;
        }
    }
    // ... (SingletonBase 클래스 시작)
```

### 1.2. `Instance` 프로퍼티 로직 변경
종료 중 (`IsQuitting`) 이라도 이미 생성된 인스턴스가 있다면 반환하도록 변경하여, 종료 과정에서 발생하는 `NullReferenceException` 을 방지했습니다.

**[Before]**
```csharp
        private static bool _applicationIsQuitting = false; // 로컬 변수 사용

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[SingletonBase] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null; // 무조건 null 반환
                }
                // ...
```

**[After]**
```csharp
        // _applicationIsQuitting 변수 제거됨 (SingletonRegistry 사용)

        public static T Instance
        {
            get
            {
                if (SingletonRegistry.IsQuitting)
                {
                    // 종료 중이라도 인스턴스가 살아있다면 반환
                    if (_instance != null)
                    {
                        return _instance;
                    }

                    Debug.LogWarning($"[SingletonBase] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }
                // ...
```

### 1.3. `OnApplicationQuit` 메서드 변경

**[Before]**
```csharp
        public void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
```

**[After]**
```csharp
        public void OnApplicationQuit()
        {
            SingletonRegistry.IsQuitting = true;
        }
```

---

## 2. `SimpleConnectionTest.cs` (안전장치 추가)

**목적:** `ClientServerHandler.Instance` 가 `null` 일 경우 발생하는 크래시를 방지하고 명확한 에러 로그를 출력.

### 2.1. `ConnectToServer` 메서드

**[Before]**
```csharp
    [ContextMenu("1. Connect to Server")]
    public async void ConnectToServer()
    {
        Debug.Log($"[Test] 서버 연결 시도: {serverAddress}:{serverPort}");

        try
        {
            await ClientServerHandler.Instance.ConnectAsync(serverAddress, serverPort);

            if (ClientServerHandler.Instance.IsConnected)
            {
                // ...
```

**[After]**
```csharp
    [ContextMenu("1. Connect to Server")]
    public async void ConnectToServer()
    {
        Debug.Log($"[Test] 서버 연결 시도: {serverAddress}:{serverPort}");

        try
        {
            var handler = ClientServerHandler.Instance;
            if (handler == null)
            {
                lastResult = "연결 오류: ClientServerHandler.Instance is null";
                Debug.LogError($"[Test] {lastResult}");
                return;
            }

            await handler.ConnectAsync(serverAddress, serverPort);

            if (handler.IsConnected)
            {
                // ...
```

### 2.2. `CheckConnection` 메서드

**[Before]**
```csharp
    private bool CheckConnection()
    {
        if (!ClientServerHandler.Instance.IsConnected)
        {
            lastResult = "서버에 연결되어 있지 않습니다.";
            Debug.LogError($"[Test] {lastResult}");
            return false;
        }
        return true;
    }
```

**[After]**
```csharp
    private bool CheckConnection()
    {
        var handler = ClientServerHandler.Instance;
        if (handler == null)
        {
            lastResult = "ClientServerHandler.Instance is null.";
            Debug.LogError($"[Test] {lastResult}");
            return false;
        }

        if (!handler.IsConnected)
        {
            lastResult = "서버에 연결되어 있지 않습니다.";
            Debug.LogError($"[Test] {lastResult}");
            return false;
        }
        return true;
    }
```
