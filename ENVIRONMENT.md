# 在无 NuGet 网络的沙箱里跑 Buffer.Benchmark

本文件记录本次基准测试所用环境的**可复现搭建步骤**（Debian 12 / x86-64，无法访问 nuget.org）。

## 1. .NET SDK：从 Google Drive 离线安装

沙箱里 `dotnet` 不存在、`api.nuget.org`/`dot.net` 均不可达，因此 SDK 走 Drive 上的分段压缩包：

```bash
# Drive 上的文件（dotnet/ 目录）：
#   dotnet-sdk-8.0.427-linux-x64.tar.zip.001/.002/.003   （Project 8 SDK 8.0.427）
#   dotnet-sdk-6.0.428-linux-x64.tar.zip.001/.002         （net6.0 TFM 的 targeting pack）
cd ~/GBuffer-cs/google_drive
cat dotnet-sdk-8.0.427-linux-x64.tar.zip.00* > /tmp/dotnet8.zip      # 拼接分段
mkdir -p /tmp/z8 && cd /tmp/z8 && unzip -q /tmp/dotnet8.zip         # 里面是一个 .tar.gz
mkdir -p ~/dotnet8 && tar xzf dotnet-sdk-8.0.427-linux-x64.tar.gz -C ~/dotnet8

# .NET 6 同理解到 ~/dotnet6，然后把 6.0.428 / 6.0.36 / packs 软链进 ~/dotnet8
for d in sdk shared/Microsoft.NETCore.App packs/Microsoft.NETCore.App.Ref \
         packs/Microsoft.AspNetCore.App.Ref packs/Microsoft.NETCore.App.Host.linux-x64; do
  for x in ~/dotnet6/$d/*; do n=$(basename "$x"); [ -e ~/dotnet8/$d/$n ] || ln -s "$x" ~/dotnet8/$d/$n; done
done

export DOTNET_ROOT=$HOME/dotnet8
export PATH=$HOME/dotnet8:$PATH
dotnet --info     # 应显示 SDK 8.0.427 + 6.0.428、Runtime 8.0.33 + 6.0.36
```

> 为什么还要 6.0？`Buffer.csproj` 是 `netstandard2.1;net8.0`、`Buffer.Benchmark.csproj` 是 `net6.0;net8.0`，
> BenchmarkDotNet 生成样板工程时会按全部 TFM 还原，缺 net6.0 targeting pack 会报 `NU1101 Microsoft.NETCore.App.Ref`。

## 2. NuGet 包：Drive 上已有的 .nupkg 组成本地源

```bash
mkdir -p ~/nuget-local
# 从 Drive 逐个下载（search_files: name contains 'benchmarkdotnet' 等）后放进 ~/nuget-local
# 本项目实际需要的：
#   benchmarkdotnet.0.15.8 / benchmarkdotnet.annotations.0.15.8
#   perfolizer.0.6.1 / pragmastat.3.2.4 / iced.1.21.0 / gee.external.capstone.2.3.0
#   commandlineparser.2.9.1 / microsoft.dotnet.platformabstractions.3.1.6
#   microsoft.codeanalysis.csharp|common.4.14.0 / microsoft.codeanalysis.analyzers.3.11.0
#   microsoft.diagnostics.runtime.3.1.512801 / .tracing.traceevent.3.1.21 / .netcore.client.0.2.510501
#   system.management.9.0.5 / system.codedom.9.0.5 / system.text.json.8.0.5
#   system.collections.immutable.9.0.0 / system.reflection.metadata.9.0.0
#   system.runtime.compilerservices.unsafe.6.1.2 / system.reflection.typeextensions.4.7.0
#   netstandard.library.2.0.3(+1.6.1) / microsoft.win32.registry.5.0.0
#   system.security.accesscontrol.5.0.0 / system.security.principal.windows.5.0.0
#   microsoft.extensions.{logging,logging.abstractions,dependencyinjection,
#     dependencyinjection.abstractions,options,primitives}.6.0.0
#   microsoft.netcore.platforms.5.0.0 / system.diagnostics.diagnosticsource.6.0.0
#   system.buffers.4.5.1 / system.memory.4.5.5 / system.numerics.vectors.4.5.0
#   system.text.encoding.codepages.6.0.0+7.0.0 / system.text.encodings.web.6.0.0+8.0.0
#   system.threading.tasks.extensions.4.5.4

# 关键是让 NuGet 只用本地源（BenchmarkDotNet 生成的样板工程会重新 restore，
# 默认源仍指向 https://api.nuget.org 会直接 NU1301 失败）
cat > ~/.nuget/NuGet/NuGet.Config <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="/home/user/nuget-local" />
  </packageSources>
</configuration>
EOF
```

`dotnet restore` 会对 `System.Runtime.CompilerServices.Unsafe 6.0.0`、`Microsoft.Diagnostics.NETCore.Client 0.2.410101`
报 `NU1603`（近似版本匹配），属于警告，不影响构建/运行。

## 3. 构建与运行

```bash
export DOTNET_ROOT=$HOME/dotnet8 PATH=$HOME/dotnet8:$PATH

cd ~/GBuffer-cs/GBuffer
dotnet build Buffer.Benchmark/Buffer.Benchmark.csproj -c Release -f net8.0 \
  -p:TargetFrameworks=net8.0 --source $HOME/nuget-local

# 单类试跑（dry job 只跑 1 次迭代，用于验证链路）
dotnet Buffer.Benchmark/bin/Release/net8.0/Buffer.Benchmark.dll \
  --filter '*CircularBufferBenchmark*' --job dry

# 全量（本次实测 41 分 42 秒 / 540 个 case）
dotnet Buffer.Benchmark/bin/Release/net8.0/Buffer.Benchmark.dll --filter '*'
```

结果落在 `Buffer.Benchmark/BenchmarkDotNet.Artifacts/results/`（每个类一份 `-report.md/.csv/.html`），
用 `toolchain/summarize.py <results-dir> <out.md>` 可汇总成一张大表（本次产物见仓库根目录 `BENCHMARKS.md`）。

## 4. 常见报错对照

| 报错 | 原因 / 处理 |
|---|---|
| `NU1301 Unable to load the service index` | 样板工程 restore 走了 nuget.org；用上面的 `NuGet.Config` 清空源、只留本地目录 |
| `NU1101 Microsoft.NETCore.App.Ref` | 缺 net6.0 targeting pack；把 SDK 6 装进同一 `DOTNET_ROOT` |
| `NU1102 System.Collections.Immutable (>= 9.0.0)` | 需要 9.0.0 版本（8.0.0 不够），补下载 |
| `Failed to set up high priority (Permission denied)` | 沙箱无法提升进程优先级，仅提示；不影响结果 |
| `--filter '*'` 之前出现交互式菜单 | 不带 filter 且无 TTY 时会要求选择；脚本里务必显式传 `--filter` |

## 5. 备注：本次环境里另外做的两件事

- 早期在 NuGet 完全不可用时，用 npm 上 `@dprint/roslyn-linux-x64-glibc@0.22.1` 里自带的 CoreCLR 10.0.11 +
  Roslyn 编译 DLL，用一个本地 mini-runner 驱动；后来 Drive 上找到真正的 SDK 与 BenchmarkDotNet 后改为标准流程，
  mini-runner 仅作为 `toolchain/` 里的备用方案保留（`toolchain/build-all.sh`）。
