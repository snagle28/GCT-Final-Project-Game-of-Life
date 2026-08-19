import random
import math
from pydub import AudioSegment

# --- 설정 값 ---
TOTAL_DURATION_MS = 4 * 60 * 1000  # 4분 (밀리초)
COLORS = ["Forest_Color1", "Forest_Color2", "Forest_Color3"]
NUM_NOTES = 12
BASE_BPM = 140
PROBABILITIES = {
    "Forest_Color1": 0.60,  # 60% 확률
    "Forest_Color2": 0.20,  # 20% 확률
    "Forest_Color3": 0.40   # 40% 확률
}

print("음원 파일을 불러오는 중입니다...")
# 1. 오디오 파일 메모리에 미리 로드 (디스크 읽기 속도 최적화)
audio_pool = {}
for color in COLORS:
    audio_pool[color] = {}
    for i in range(1, NUM_NOTES + 1):
        filename = f"{color}_{i}.wav"
        try:
            audio_pool[color][i] = AudioSegment.from_wav(filename)
        except FileNotFoundError:
            print(f"경고: {filename} 파일을 찾을 수 없습니다. 무음 처리됩니다.")
            audio_pool[color][i] = AudioSegment.silent(duration=500)

# 2. 상태 초기화
# 노트는 중간 대역(6)에서 시작
current_notes = {color: 6 for color in COLORS}

# 패닝(좌우)을 위한 위상과 속도 (각 컬러별로 서로 다른 주기로 좌우로 움직임)
pan_phases = {color: random.uniform(0, 2 * math.pi) for color in COLORS}
pan_speeds = {color: random.uniform(0.02, 0.05) for color in COLORS} 

# 5분짜리 빈(무음) 캔버스 생성
output = AudioSegment.silent(duration=TOTAL_DURATION_MS + 2000)
current_time_ms = 0
bpm_phase = 0

print("4분짜리 랜덤 사운드스케이프를 생성하는 중입니다. 잠시만 기다려주세요...")

# 3. 시간 순서대로 믹싱 루프 (그리드 기반)
while current_time_ms < TOTAL_DURATION_MS:
    # [조건 1] 템포 변화: BPM이 120 ~ 160 사이를 아주 서서히 왕복함 (사인파 활용)
    current_bpm = BASE_BPM + (math.sin(bpm_phase) * 20)
    # 현재 BPM 기준 8분음표(Half beat) 길이 계산
    step_duration_ms = (60000 / current_bpm) / 2 
    bpm_phase += 0.05  # 템포 변화 속도

    # 각 스텝(그리드)마다 각 컬러(1, 2, 3)가 소리를 낼지 말지 결정
    # [조건 2] Color1, 2, 3은 독립적으로 굴러가므로 0개~3개의 음이 동시에 나올 수 있음
    for color in COLORS:
        # 각 컬러당 해당 스텝에서 소리가 날 확률
        # 확률을 조절하여 너무 비거나 너무 꽉 차지 않게 조율 (원하시면 수치 변경)
        if random.random() < PROBABILITIES[color]:
            
            # [조건 3] 연속적인 움직임 (Random Walk): 현재 번호에서 -1, 0, +1 중 하나만 이동
            move = random.choice([-1, 0, 1])
            new_note = current_notes[color] + move
            # 1~12 범위를 벗어나지 않도록 고정
            new_note = max(1, min(NUM_NOTES, new_note))
            current_notes[color] = new_note
            
            sound = audio_pool[color][new_note]
            sound = sound-6
            
            # [조건 4] 연속적인 패닝: 사인파를 사용하여 시간이 지남에 따라 좌(-1) ~ 우(1)로 부드럽게 이동
            time_sec = current_time_ms / 1000.0
            pan_val = math.sin((time_sec * pan_speeds[color]) + pan_phases[color])
            
            # 패닝 적용 및 메인 캔버스에 얹기 (믹싱)
            panned_sound = sound.pan(pan_val)
            output = output.overlay(panned_sound, position=current_time_ms)

    # 다음 비트로 시간 이동
    current_time_ms += step_duration_ms

# 4. 마무리 (페이드 아웃) 및 파일 저장
print("믹싱 완료! 파일을 저장합니다...")
# output = output + AudioSegment.silent(duration=2000)
output.export("Forest_R_1.wav", format="wav")

print("✨ 성공적으로 'Forest_R_1.wav' 파일이 생성되었습니다!")