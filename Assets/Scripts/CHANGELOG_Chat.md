# 인게임 채팅 시스템 변경사항 (Coder B - JY)

> 작업일: 2026-02-11
> 브랜치: feat/final_item (develop 기반)

---

## 변경 파일 목록

| 파일 | 액션 | 위험도 |
|------|------|--------|
| `ChatUIController.cs` | 죽은 플레이어 입력 차단 추가 | LOW |
| `TestScene_Main.unity` | 채팅 UI 오브젝트 추가 (씬 작업) | LOW |

**안 건드린 파일:** LobbyChatManager.cs, Scene_Lobby.unity

---

## 1. ChatUIController.cs — 죽은 플레이어 전송 차단

**목적:** 인게임에서 죽은 플레이어는 채팅창을 볼 수는 있지만 메시지를 보낼 수 없게

### 변경 내용
- `using Photon.Pun;` 추가
- `IsLocalPlayerDead()` 메서드 추가 — `CustomProperties["IsDead"]` 체크
- `ToggleChatPanel()` — 패널은 열리되, 죽은 상태면 `inputField.interactable = false`, `sendButton.interactable = false`
- `OnSend()` — 죽은 상태면 전송 자체 차단 (안전장치)

### 동작
| 상태 | 채팅창 열기 | 메시지 읽기 | 메시지 전송 |
|------|------------|------------|------------|
| 생존 | O | O | O |
| 사망 | O | O | X (입력/전송 비활성) |

### 로비 영향
- 로비에서는 `IsDead` 프로퍼티가 없거나 `false`이므로 기존과 동일하게 동작

---

## 2. TestScene_Main.unity — 씬에 채팅 오브젝트 추가

**추가된 오브젝트:**
- `Canvas/ChatPanelRoot` — 채팅 패널 (SendButton, ChatInputField, ChatScrollView)
- `Canvas/ChatButton` — 채팅 토글 버튼
- `LobbyChatManager` — Photon RPC 채팅 매니저 (Canvas 밖, 최상위)

**연결 설정 (Inspector):**
- ChatButton > On Click > ChatUIController.ToggleChatPanel
- ChatUIController > Lobby Chat > LobbyChatManager 오브젝트
- 나머지 필드 (chatPanelRoot, inputField, sendButton, scrollRect, content, chatMessagePrefab) 연결 확인
