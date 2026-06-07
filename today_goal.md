# Today's Goals

## 1. Environment 뒤로 가기 (Back to Environment Select)

**목표:** Soundscape1(Forest) 또는 Soundscape2(Ocean)에서 플레이 중
"뒤로 가기" 버튼을 누르면 Environment 선택 장면(`Soundscape Menu`)으로
돌아가고, 거기서 다른 Environment를 다시 고를 수 있게 한다.

**현황 / 관련 코드:**
- 선택 장면: **`Intro Sound`** (빌드 index 1, `MoveToSoundscape1/2` 버튼 둘 다 있음).
  `Soundscape Menu.unity`는 어디서도 로드 안 하는 미사용 씬이라 타깃 아님.
- 진입 경로: `SceneManagement.MoveToSoundscape1()` / `MoveToSoundscape2()`
  (`Assets/SceneManagement.cs`) — 페이드 후 `SceneManager.LoadScene(...)`.

**구현:**
- [x] `SceneManagement.MoveToSelect()` 추가 — `Intro Sound`로 페이드 복귀.
- [x] **"click to begin" 스킵**: `SceneManagement.skipIntro` static 플래그.
      `Intro Sound` 흐름 = (click to begin 루프) → begin 클릭(`buttonClicked`) →
      디렉터 6s 점프 = "Select a Soundscape Environment" 화면. 씬에 SceneManagement가
      4개라, `director != null`인 인스턴스만 플래그를 소비하도록 게이트(레이스 방지).
      `Start`에서 `buttonClicked=true` → begin 자동 클릭 효과 → 선택 화면 직행.
- [x] `Assets/Sonification/BackButton.cs` — 런타임에 **오른쪽 아래** 테마
      아이콘 버튼(왼쪽 화살표, `Resources/UI/BackButtonIcon`) 생성, 클릭 시 `MoveToSelect()`.
- [x] 테마 프리팹: `LeftBlackButton` → `Resources/UI/BackButtonIcon.prefab` 복사.
- [x] `GameManager.Start`에서 Soundscape1/2 씬일 때만 자동 생성 (배선 불필요).
- [x] Unity Play로 동작 확인 완료 — ◄ 클릭 시 "Select a Soundscape Environment" 직행.
- [x] 버튼 크기 56×56로 축소. **1번 완료.**

---

## 2. 너무 discrete 한 소리 보완 (Smoother / Less Discrete Sound)

**문제:** 현재 멜로디는 세대마다 색별로 **한 음씩 끊어서** 재생
(`MelodyPlayer.cs` — 12단계로 양자화된 샘플을 `src.Play()`).
세대 펄스가 또렷이 들려서 음악이 계단식(discrete)으로 느껴짐.

**보완 아이디어 (논의/실험용):**
- [ ] **연속 피치(글라이드/포르타멘토):** 음을 매번 새로 트리거하지 말고
      지속음 보이스의 `AudioSource.pitch`를 mean Y에 따라 부드럽게 보간.
      → 12단계 양자화 제거, 계단음 → 연속음.
- [ ] **레가토 / 크로스페이드:** 음이 바뀔 때 이전 음을 즉시 끊지 말고
      볼륨을 겹쳐 페이드(release tail)해서 음끼리 이어지게.
- [ ] **양자화 단계 늘리기:** 색당 12음 → 더 촘촘하게(또는 반음 단위)
      해서 점프 폭을 줄임.
- [ ] **지속 패드(drone) 레이어:** 각 색마다 루프되는 부드러운 패드를
      깔고 mean Y/X로 피치·팬·볼륨만 연속 변조 (배경 사운드스케이프처럼).
- [ ] **리버브/딜레이:** 잔향을 추가해 끊긴 음들 사이를 메움.

**결정:** 방향 ② (polyphonic "서스테인 페달" + 페달 지속 시간 조절) 채택.
4초 자연 감쇠 샘플을 겹쳐 울리게 두고, 페달 시간/색당 보이스 수로 텍스처 조절.

**결정 갱신:** ②만으론 "매 세대 재-어택" 펄스가 남아 여전히 discrete.
→ ③ 레가토 하이브리드로 전환.

**구현 (③ 레가토):**
- [x] `MelodyPlayer.cs` 재작성 — **변할 때만 재발음**(같은 음이면 안 때리고 유지).
- [x] 음 바뀌면 이전 음과 **크로스페이드**(레가토). `melodyCrossfadeSeconds`.
- [x] 유지 중인 음은 **pan만 부드럽게 따라 이동**(어택 없이 살아있는 느낌).
- [x] 새 음 **어택 fade-in**으로 시작 둥글게. `melodyAttackSeconds`.
- [x] `melodySustainSeconds`(페달 유지) / `melodyReleaseSeconds`(꼬리 페이드).
- [x] `melodyMaxVoicesPerColor` 안전 cap 유지.
- [ ] Unity Play로 들어보고 sustain/crossfade/attack 튜닝.
