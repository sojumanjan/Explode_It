# 대사 글자마다 나는 짧은 웅얼거림(동물의 숲식 목소리) 클립을 합성하는 스크립트
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/voice_blip.py
# 소리 하나만 만들고, 캐릭터마다 다른 목소리는 SoundData의 피치 범위로 낸다.
# 처음엔 모음 넷(아, 에, 오, 이)을 섞었는데 발음이 바뀌는 게 거슬려(사용자) "아" 하나로 줄였다.
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets/_Project/06. Audio/대사 목소리"
RATE = 44100
LENGTH = 0.075      # 한 음절 길이(초). 초당 14글자 안팎으로 나와도 다음 소리와 크게 겹치지 않게 짧게
F0 = 190.0          # 기본 높이(Hz). 피치 0.6~1.6으로 낮은 보스부터 높은 고블린까지 덮는 가운데
GLIDE = 0.9         # 끝으로 갈수록 이 비율까지 높이가 살짝 내려가 말끝처럼 들린다
ATTACK = 0.006      # 소리가 차오르는 시간(초). 짧아야 글자마다 또렷하게 끊긴다
PEAK = 0.95         # 최대 크기(0~1). 0.55는 작게 들렸다(사용자)

# "아" 모음 공명(포먼트) 주파수와 폭(Hz). 실제 사람 모음 값을 단순화했다.
FORMANTS = [(800, 80), (1200, 90), (2500, 120)]


def pulse_train(n: int) -> np.ndarray:
    # 성대 떨림 대신 부드러운 톱니파. 높이가 조금씩 내려간다.
    t = np.arange(n) / RATE
    f = F0 * (1 + (GLIDE - 1) * t / LENGTH)
    phase = np.cumsum(f) / RATE
    saw = 2 * (phase % 1.0) - 1
    # 톱니의 날카로운 끝을 살짝 깎아 귀가 따갑지 않게
    return np.convolve(saw, np.ones(4) / 4, mode="same")


def resonate(x: np.ndarray, freq: float, bw: float) -> np.ndarray:
    # 2차 공명 필터 하나(모음 공명 한 개)
    r = np.exp(-np.pi * bw / RATE)
    c = 2 * r * np.cos(2 * np.pi * freq / RATE)
    y = np.zeros_like(x)
    y1 = y2 = 0.0
    g = 1 - r
    for i, v in enumerate(x):
        y0 = g * v + c * y1 - r * r * y2
        y[i] = y0
        y2, y1 = y1, y0
    return y


def envelope(n: int) -> np.ndarray:
    t = np.arange(n) / RATE
    rise = np.clip(t / ATTACK, 0, 1)
    fall = np.clip(1 - (t - ATTACK) / (LENGTH - ATTACK), 0, 1) ** 1.6
    return rise * fall


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    n = int(RATE * LENGTH)
    src = pulse_train(n)
    y = sum(resonate(src, f, bw) for f, bw in FORMANTS)
    y *= envelope(n)
    y = y / np.abs(y).max() * PEAK
    pcm = (y * 32767).astype(np.int16)
    path = OUT / "목소리.wav"
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    print(path.name, n)


if __name__ == "__main__":
    main()
