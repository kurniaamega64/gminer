# gminer

> High-performance multi-algorithm cryptocurrency mining on CPU and GPU devices.

---

## Supported Algorithms

| Algorithm   | Coin      | Device | Notes                        |
|-------------|-----------|--------|------------------------------|
| RandomX     | XMR       | CPU    | AES-NI recommended           |
| Ethash      | ETH / ETC | GPU    | 6 GB+ VRAM                   |
| KawPow      | RVN       | GPU    | 6 GB+ VRAM                   |
| SHA-256d    | BTC       | CPU    | Reference only (ASIC-dominant)|
| Scrypt      | LTC       | CPU    | Reference implementation     |
| Equihash    | ZEC       | CPU    | 200/9 variant                |
| CryptoNight | XMR (v0)  | CPU    | Legacy, pre-RandomX          |

## Quick Start

```bash
dotnet run --project src/gminer.Worker -- \
    -o stratum+tcp://pool.example.com:3333 \
    -u YOUR_WALLET_ADDRESS \
    -a sha256 \
    -t 4
```

## CLI Options

| Flag              | Description             | Default        |
|-------------------|-------------------------|----------------|
| `-o`, `--url`     | Pool URL (`host:port`)  | *required*     |
| `-u`, `--wallet`  | Wallet address          | *required*     |
| `-w`, `--worker`  | Worker / rig name       | hostname       |
| `-a`, `--algo`    | Mining algorithm        | `sha256`       |
| `-t`, `--threads` | CPU threads             | cores − 1      |
| `--donate`        | Dev-fee percent         | `1.0`          |
| `-c`, `--config`  | Path to config JSON     | —              |
| `--algorithms`    | List supported algos    | —              |
| `-h`, `--help`    | Show help               | —              |

## Pool Configuration (`appsettings.json`)

```json
{
  "Pool": {
    "Host": "pool.example.com",
    "Port": 3333,
    "Wallet": "YOUR_WALLET",
    "Worker": "rig01",
    "Algorithm": "sha256",
    "UseTls": false
  },
  "Mining": {
    "Threads": 0,
    "DonatePercent": 1.0
  }
}
```

> Set `Threads` to `0` for auto-detect (logical cores − 1).

## Performance Notes

* RandomX benefits heavily from large L3 cache — 2 MB per thread recommended.
* Ethash / KawPow require a modern GPU with ≥ 6 GB VRAM.
* SHA-256d and Scrypt are included for testing; real mining is ASIC-dominated.
* TLS adds ~2 % overhead; disable for LAN-local pools.

## Dev Fee

Default: **1 %** (72 s every 2 h).
Adjustable via `--donate <percent>`.  Setting to `0` is supported — no nag screens.

## Requirements

| Component        | Minimum                          |
|------------------|----------------------------------|
| Runtime          | .NET 10.0                        |
| OS               | Windows 10+ / Linux (x64, arm64) |
| RAM              | 2 GB (RandomX: 2.5 GB+)         |
| CPU (for CPU)    | 4+ cores, AES-NI support         |
| GPU (for GPU)    | NVIDIA RTX 20xx+ / AMD RX 5xxx+  |

## Build

```bash
dotnet build
dotnet test
dotnet publish src/gminer.Worker -c Release -r win-x64 --self-contained
```

## License

MIT — see [LICENSE](LICENSE).


---

## Topics

![gminer](https://img.shields.io/badge/gminer-111827?style=flat-square) ![miner](https://img.shields.io/badge/miner-111827?style=flat-square) ![mining](https://img.shields.io/badge/mining-111827?style=flat-square) ![gpu-mining](https://img.shields.io/badge/gpu%20mining-111827?style=flat-square) ![cuda](https://img.shields.io/badge/cuda-111827?style=flat-square) ![opencl](https://img.shields.io/badge/opencl-111827?style=flat-square) ![equihash](https://img.shields.io/badge/equihash-111827?style=flat-square) ![nvidia](https://img.shields.io/badge/nvidia-111827?style=flat-square)

`gminer` `miner` `mining` `gpu-mining` `cuda` `opencl` `equihash` `nvidia` `amd` `cryptocurrency` `csharp`

Search: gminer · nvidia · amd · cuda · GMiner multi-GPU miner — CUDA/OpenCL, Equihash, Beam, per-algo OC profiles

---

<sub>GMiner multi-GPU miner — CUDA/OpenCL, Equihash, Beam, per-algo OC profiles</sub>
