# Unity 사용법 (처음 만지는 사람용 가이드)

> Unity를 한 번도 열어본 적이 없는데 이 레포의 sonification 기능을 돌려야 한다면, 이 문서를 위에서 아래로 따라가세요. 약 10분 소요.

English version: `HOW_TO_USE_UNITY.md`

## Unity Editor 패널 빠른 정리

- **Hierarchy** (왼쪽 위): 현재 씬의 GameObject 목록
- **Scene view** (가운데): GameObject 배치/확인하는 시각 편집 화면
- **Game view** (가운데 탭): 재생(Play) 버튼 눌렀을 때 실제로 보이는 화면
- **Inspector** (오른쪽): 선택한 GameObject 또는 에셋의 속성
- **Project** (아래): `Assets/` 폴더 파일 브라우저
- **Console** (Project 옆 탭): 에러/로그 출력. 항상 켜둘 것

## 0. 시작 전 준비

1. **Unity Hub**에서 이 프로젝트 열기 (Unity 6 / Unity 6000.x 권장, 빌드 환경은 Unity 6.4).
2. 프로젝트가 열리면 우측 하단 스피너가 멈출 때까지 대기. Unity가 mp3 파일과 새 C# 스크립트를 import 중입니다.
3. 메뉴 **Window → General → Console** 열어두기. 빨간 에러가 있으면 그것부터 해결하세요 — 안 그러면 아래 단계가 동작하지 않습니다.

## 1. 메인 씬 열기

**Project** 패널(아래)에서 `Assets/Scenes/`로 가서 씬 파일 더블클릭 (보통 `Main Scene` 또는 `SampleScene`).

열리면 보이는 것:
- 가운데 **Scene view**에 그리드
- 왼쪽 **Hierarchy**에 **`GameManager`**라는 GameObject

> **이름 주의 (한 번만 읽으면 평생 안 헷갈림)**: Hierarchy의 GameObject 이름은 `GameManager`입니다. 거기 붙어있는 C# 스크립트는 `GameOfLifeManager` 클래스입니다. Unity는 슬롯 타입을 GameObject 이름이 아니라 **스크립트 클래스**로 매칭합니다. 그래서 슬롯 라벨에 `Game Of Life Manager`라고 적혀있어도, `GameManager` GameObject를 드래그하면 Unity가 받아줍니다 — `GameManager`에 `GameOfLifeManager` 스크립트가 붙어있기 때문입니다. 이 가이드 전체에서 같은 규칙이 적용됩니다.

## 2. ChordPalette 에셋 만들기

`ChordPalette`는 코드 → 오디오 클립 매핑을 들고 있는 에셋입니다. 오디오팀이 사운드를 교체할 때 이 파일을 수정합니다.

1. **Project**에서 `Assets/Sonification/` 열기
2. 빈 공간 우클릭 → **Create → Sonification → Chord Palette**
3. 이름을 `MainChordPalette`로 변경

> **Sonification** 메뉴가 안 보이면 컴파일이 안 끝났거나 에러가 있는 것입니다. Console 확인.

`MainChordPalette` 클릭하면 Inspector에 설정 화면이 뜹니다:

4. `Chords` → `Size`를 **3**으로
5. 세 개 항목 채우기 (순서 중요 — 아래 경고 참고):

   | Index | Label     | Notes (Project의 `Assets/Sounds/`에서 드래그)   |
   |-------|-----------|-------------------------------------------------|
   | 0     | `C major` | `C_1.mp3`, `C_2.mp3`, `C_3.mp3`                 |
   | 1     | `F major` | `F_1.mp3`, `F_2.mp3`, `F_3.mp3`                 |
   | 2     | `G major` | `G_1.mp3`, `G_2.mp3`, `G_3.mp3`                 |

> **순서가 중요합니다**: index 0은 2-이웃 셀의 코드, 1은 3-이웃 셀, 2는 그 외. 이게 `GameOfLifeManager.GetChordIndex`와 맞아야 합니다. 순서가 틀리면 색깔별로 잘못된 코드가 재생됩니다.

## 3. SonificationConfig 에셋 만들기

튜닝 가능한 숫자들(트리거 주기, loudness 포화점, 최소 볼륨) 보관용 에셋입니다.

1. **Project**의 `Assets/Sonification/`에서 우클릭 → **Create → Sonification → Config**
2. 이름 `MainSonificationConfig`

첫 테스트에는 기본값 그대로 OK:

- `Trigger Every N Ticks` = 2
- `Loudness Saturation Count` = 8
- `Minimum Audible Volume` = 0.02

## 4. VoicePool GameObject 만들기

`VoicePool`은 런타임 오디오 재생 엔진입니다 — 시작할 때 코드×노트 슬롯 하나당 `AudioSource`를 하나씩 생성합니다.

1. **Hierarchy**에서 빈 공간 우클릭 → **Create Empty**
2. 새로 생긴 GameObject 이름을 **`VoicePool`**로 변경 (F2 또는 이름 더블클릭)
3. `VoicePool` 선택 상태에서, **Inspector** 하단 **Add Component** 클릭
4. 검색창에 `VoicePool` 입력 → 결과 클릭해서 컴포넌트 추가
5. 컴포넌트에 `Palette` 슬롯이 보이면, **Project**에서 `MainChordPalette`를 드래그해서 놓기

## 5. Sonifier GameObject 만들기

`Sonifier`는 글루(glue)입니다 — Game of Life 매 틱마다 호출되어 sonification 파이프라인을 돌립니다.

1. **Hierarchy**에서 우클릭 → **Create Empty** → 이름을 **`Sonifier`**로
2. **Inspector** → **Add Component** → `Sonifier` 검색 → 추가
3. 네 개의 참조 슬롯 채우기 (전부 드래그, 타이핑 X):

   | 슬롯         | 드래그할 것                                                                            |
   |--------------|----------------------------------------------------------------------------------------|
   | `Game`       | Hierarchy → **`GameManager`** (GameObject — Unity가 스크립트 클래스로 매칭, §1 참고)   |
   | `Palette`    | Project → **`MainChordPalette`**                                                        |
   | `Config`     | Project → **`MainSonificationConfig`**                                                  |
   | `Voice Pool` | Hierarchy → **`VoicePool`**                                                             |

다 채우면 "None (...)" 글자가 사라지고 각 슬롯에 GameObject/에셋 이름이 표시됩니다.

## 6. Sonifier를 GameManager에 연결

시뮬레이션이 매 틱마다 어떤 Sonifier를 호출할지 알려줘야 합니다.

1. **Hierarchy**에서 **`GameManager`** 클릭
2. **Inspector**에서 `Game Of Life Manager` 컴포넌트 안의 **`Sonification`** 헤더 찾기
3. 그 아래 비어있는 **`Sonifier`** 슬롯에, **Hierarchy의 `Sonifier` GameObject**를 드래그

## 7. 오디오 출력 확인

재생 누르기 전 점검 사항:

- **Hierarchy**에서 **Main Camera** 클릭 → Inspector에 **Audio Listener** 컴포넌트가 있는지 확인. (기본 Unity 씬엔 있음. 없으면 **Add Component → Audio Listener**.)
- **Game view** 상단의 스피커 아이콘(**Mute Audio**)이 **꺼져있는지** 확인 (강조 안 된 상태).
- OS 볼륨 켜두기.

## 8. 재생 + 테스트

1. 에디터 상단의 **▶ Play** 버튼 클릭
2. **Space**키로 일시정지 해제 (기본은 paused 상태로 시작)
3. 마우스로 그리드에 살아있는 셀을 그리거나, 단축키 사용:
   - `G` — 커서 위치에 글라이더 배치
   - `M` — 클리어 후 P101 패턴 배치
4. 셀이 진화하면서 매 2틱마다 코드 노트가 트리거되는 게 들려야 합니다. 볼륨은 행별 동일 색 셀 수에 비례.

## 트러블슈팅

| 증상 | 확인 사항 |
|------|-----------|
| 아예 소리가 안 남 | Game view Mute 꺼져있는지, OS 볼륨, Main Camera에 Audio Listener |
| Console에 빨간 에러 | 에러 메시지 읽기. 보통 빠진 참조 또는 슬롯 미할당. 진행 전에 해결 |
| 화면은 변하는데 소리 없음 | (Step 6) `GameManager`의 `Sonifier` 슬롯이 비어있을 가능성 큼 |
| Create 메뉴에 `Sonification` 없음 | 스크립트 컴파일이 안 됐거나 에러. Console 확인 |
| 너무 시끄럽거나 거침 | `MainSonificationConfig`의 `Loudness Saturation Count`를 16 또는 32로 ↑ |
| 너무 빽빽함, 박자감 없음 | `Trigger Every N Ticks`를 3 또는 4로 ↑, 그리고/또는 재생 중 `S`로 시뮬레이션 속도 ↓ |
| 듬성듬성하고 조용한 노트만 들림 | `Minimum Audible Volume`을 0.05 또는 0.1로 ↑ |
| 색깔별로 잘못된 코드가 재생됨 | `MainChordPalette`의 항목 순서가 틀림. 반드시 0=C, 1=F, 2=G |

## 다음으로 볼 만한 것

- `../reference/ARCHITECTURE.md` — 시스템 아키텍처 (3단계 파이프라인, 팀 역할, 수정 가이드)
- `../plans/PLAN_2026_05_21.md` — v1 스프린트 계획 (구현된 단계와 결정)
- `../log/LOG.md` — 시간순 작업 기록과 그 이유
- `../reference/REPORT.md` — Wwise 같은 대안 대신 이 오디오 스택을 선택한 이유
- `../reference/MPTK.md` — 검토했지만 채택하지 않은 MIDI 기반 대안
