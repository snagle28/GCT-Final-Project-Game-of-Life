# Sonification Tuning Reference

소리 관련 조정 변수 모음. 전부 **`GameManager` 컴포넌트의 Inspector → "Sonification" 섹션**에 있고,
**Play 중에도 실시간으로 적용**됩니다 (값은 호출 시마다 읽음).

- 환경 매핑: Forest = Soundscape1 = env1, Ocean = Soundscape2 = env2
- 색 인덱스: 0 = Yellow/C, 1 = Beige/F, 2 = Blue/G
- 코드 위치: `Assets/GameManager.cs` (필드 선언) / `Assets/Sonification/*.cs` (동작)

---

## AD1 / AD2 — 멜로디 (세대별 음, 레가토)

색마다 "건반 하나를 누르고 있는" 느낌. 음(pitch)은 그 색 셀들의 **평균 Y**(아래=낮음, 위=높음),
스테레오는 **평균 X**(왼쪽=−1, 오른쪽=+1). 음이 **바뀔 때만** 새로 치고, 안 바뀌면 유지(레가토).

| Inspector 이름 | 코드 필드 | 범위 | 기본값 | 설명 |
|---|---|---|---|---|
| Melody Volume | `melodyVolume` | 0 – 1 | **0.6** | 멜로디 음 크기 |
| Melody Pan Strength | `melodyPanStrength` | 1 – 4 | **2.5** | 스테레오 벌어짐 (클수록 좌우 극단적) |
| Melody Min Interval | `melodyMinInterval` | 0 – 0.5s | **0.12** | 멜로디 재평가 최소 간격 (템포와 분리; 빠를 때 음 뭉침 방지) |
| Melody Sustain Seconds | `melodySustainSeconds` | 0.1 – 4s | **3.5** | 유지된 음이 페이드아웃되기 전까지 울리는 시간 ("페달") |
| Melody Crossfade Seconds | `melodyCrossfadeSeconds` | 0.02 – 1s | **0.3** | 음이 바뀔 때 이전 음과 겹쳐 넘어가는 길이 (클수록 부드러움) |
| Melody Attack Seconds | `melodyAttackSeconds` | 0 – 0.5s | **0.08** | 새 음 시작의 fade-in (클수록 "땅!" 어택이 둥글어짐) |
| Melody Release Seconds | `melodyReleaseSeconds` | 0.02 – 1s | **0.3** | 유지 시간이 끝난 음의 꼬리 페이드아웃 길이 |
| Melody Max Voices Per Color | `melodyMaxVoicesPerColor` | 1 – 10 | **6** | 색당 동시에 겹칠 수 있는 음 수 안전 상한 (초과 시 오래된 음 회수) |

**자주 쓰는 조정**
- **반복(같은 음) 주기**를 늘리고 싶다 → `Melody Sustain Seconds` ↑ (반복 간격 ≈ Sustain + Release).
  단 샘플이 ~4초라 4초가 천장 — 그보다 길게는 현재 불가(별도 gap 파라미터 필요).
- **더 부드럽게** → `Melody Crossfade Seconds` ↑ (0.4–0.6), `Melody Attack Seconds` ↑ (0.15–0.25).
**Y축 → 소리 매핑 (쉽게 변경)**
- 음원: `Resources/Notes/{Forest,Ocean}/`, 파일명 `{Env}_Color{색}_{번호}` (예: `Forest_Color1_1`).
- **번호 `_1`이 최저음(맨 아래 Y), 마지막 번호가 최고음(맨 위 Y).**
- **색당 음 개수는 고정이 아니라 폴더의 파일 개수대로 자동** 결정됩니다 (색마다 달라도 됨).
  Y축은 그 개수만큼 자동 분할. → 파일을 **추가/삭제/번호변경**만 하면 매핑이 바뀜.
- 제약: 개수가 grid 행 수(`gridSize`, 현재 50)를 넘으면 grid 수로 cap (그 이상은 의미 없음).
- 현재 기본: Forest/Ocean 각 색 12개.

---

## AD3 — 클릭 효과음 (셀 그릴 때)

셀을 칠하는 순간(죽→삶) footstep 샘플 무작위 1개 재생.

| Inspector 이름 | 코드 필드 | 범위 | 기본값 | 설명 |
|---|---|---|---|---|
| Footstep Volume | `footstepVolume` | 0 – 1 | **1.0** | 클릭 효과음 크기 |

**음원**: `Resources/Footsteps/{Forest,Ocean}/`

---

## AD4 — 시작/정지 효과음 (스페이스바 토글)

재생/일시정지 토글마다(둘 다) 효과음 1회.

| Inspector 이름 | 코드 필드 | 범위 | 기본값 | 설명 |
|---|---|---|---|---|
| Start Sound Volume | `startSoundVolume` | 0 – 1 | **1.0** | 시작/정지 효과음 크기 |

**음원**: `Resources/StartSounds/{Forest,Ocean}/` (각 1개: Forest=bird, Ocean=ship)

---

## AD5 — 배경 루프 (살아있는 셀 수에 따라 볼륨)

배경 사운드스케이프가 계속 루프하며, 전체 살아있는 셀 수 비율로 볼륨이 변함.

| Inspector 이름 | 코드 필드 | 범위 | 기본값 | 설명 |
|---|---|---|---|---|
| Bg Min Volume | `bgMinVolume` | 0 – 1 | **0.1** | 셀 0개일 때 볼륨 |
| Bg Max Volume | `bgMaxVolume` | 0 – 1 | **0.8** | 셀이 cap에 도달했을 때 볼륨 |
| Bg Cap Fraction | `bgCapFraction` | 0.01 – 1 | **0.3** | cap = 전체 셀 수의 이 비율 (이만큼 차면 최대 볼륨) |
| Bg Smoothing | `bgSmoothing` | 0.01 – 1 | **0.1** | 프레임당 볼륨 보간 (작을수록 천천히 변함, 튐 방지) |

**음원**: `Resources/Soundscapes/{Forest,Ocean}/` (대용량 13–24MB, 루프)

---

## 참고 — 코드 매핑

| 레이어 | 동작 스크립트 | 호출 위치 |
|---|---|---|
| AD1/AD2 | `Sonification/MelodyPlayer.cs` | `GameManager.NextGeneration` (재생), `GameManager.Update` (보이스 관리) |
| AD3 | `Sonification/FootstepPlayer.cs` | `GameManager.PaintCell` (죽→삶) |
| AD4 | `Sonification/StartSoundPlayer.cs` | `GameManager.HandleInput` (스페이스) |
| AD5 | `Sonification/BackgroundLoopPlayer.cs` | `GameManager.Update` (매 프레임 볼륨) |

각 플레이어는 `GameManager.Start`에서 자동 생성되어 활성 씬에 맞는 클립을 Resources에서 로드합니다
(Inspector 수동 배선 불필요).
