
# VITURE Luma（無印） 3DoFトラッキング用 USB HID プロトコル仕様（調査ベース）

**目的**  
VITURE Luma（無印）を **Windows ホスト**から **USB HID（hidapi）** で扱い、**3DoF（姿勢のみ）** のIMUデータ（オイラー角 or クォータニオン）を取得するための「接続後にどうすればよいか分かる」資料を提供する。  
 
**注意（重要）**  
本資料は、公開SDKの上位APIではなく、主に **WebHID向けにリバースエンジニアリングされた仕様**と実装（JavaScript）を根拠にしています。ファームウェア更新等で挙動が変わる可能性があります。根拠は末尾の「出典」を参照。 :contentReference[oaicite:0]{index=0}

---

## 1. スコープ

- 対象：**VITURE系グラス** （Luma無印・Pro、One系列等）
- 取得したいデータ：**3DoF姿勢（orientation）**
  - 位置（pos）や6DoFは対象外
- ホスト：**Windows**（hidapi利用可能）
- インタフェース：**USB HID（Vendor-specific）**

---

## 2. デバイス同定（VID/PID）

VITURE系グラスは以下の **USB Vendor ID / Product ID** として見える（リバースエンジニアリング資料より）。 :contentReference[oaicite:1]{index=1}

- **Vendor ID (VID)**: `0x35CA` :contentReference[oaicite:2]{index=2}

### 2.1 サポート対象モデルと Product ID (PID)

| モデル | Product IDs |
|-------|-------------|
| **VITURE One** | `0x1011`, `0x1013`, `0x1017` |
| **VITURE One Lite** | `0x1015`, `0x101b` |
| **VITURE Pro** | `0x1019`, `0x101d` |
| **VITURE Luma Pro** | `0x1121`, `0x1141` |
| **VITURE Luma（無印）** | `0x1131` |

> **注意**：各PIDは同一モデルの複数の内部リビジョン（USB記述子構成の異なるファームウェア等）を表す場合があります。ホストはすべてのPIDに対応すべきです（以下「3.」参照）。 :contentReference[oaicite:4]{index=4}

---

## 3. HIDデバイス構成（重要：2つのHIDインタフェース）

VITUREグラスは **2つのHIDインタフェース**を露出し、ホスト側（WebHID/Windows）では **別々のHIDデバイス**として見える、という前提で実装されています。 :contentReference[oaicite:5]{index=5}

- **IMU Interface（Interface 0）**：IMU姿勢データを **受信**する（ストリーム）
- **MCU Interface（Interface 1）**：IMUストリーム開始などの **コマンド送信** と ACK 受信

両方のインタフェースは以下の共通属性を持つ（報告書より）。 :contentReference[oaicite:6]{index=6}

- Usage Page: `0xFF00`（Vendor-specific）
- Usage: `0x01`
- Report ID: `0x00`
- Report Size: **64 bytes**

> **実務的には**：Windows(hidapi)では「Interface番号」を直接取りにくいことが多いので、  
> ① VID/PIDで列挙し、該当するHIDデバイスを **全部 open**  
> ② **MCU用コマンドを“全デバイスに投げる”**（受け付けるものがMCU側）  
> ③ **入力レポートを読んで** `0xFF 0xFC` ヘッダのものをIMUとして扱う  
> …が堅牢です（WebHID実装もこの方針）。 :contentReference[oaicite:7]{index=7}

---

## 4. 全体フロー（接続〜IMU受信）

### 4.1 シーケンス（推奨）

```mermaid
sequenceDiagram
  participant Host as Windows App (hidapi)
  participant HID0 as HID Dev A (likely IMU IF)
  participant HID1 as HID Dev B (likely MCU IF)

  Host->>Host: hid_enumerate(VID=0x35CA, PID=0x1131)
  Host->>HID0: hid_open_path(...)
  Host->>HID1: hid_open_path(...)

  Note over Host: IMUストリーム開始コマンドを送る（ReportID=0）
  Host->>HID0: write(OutputReport: MCU CMD 0x15 enable)
  Host->>HID1: write(OutputReport: MCU CMD 0x15 enable)

  Note over HID1: MCU IF が受理しACK返すことがある
  HID1-->>Host: InputReport (header 0xFF 0xFD) [optional]

  Note over HID0: IMU IF から IMU packet が流れる
  HID0-->>Host: InputReport (header 0xFF 0xFC) [64 bytes]
  Host->>Host: parse Euler @ payload offset 0x12
  Host->>Host: (option) Euler->Quaternion / recenter
````

根拠：IMU enable コマンド（CMD `0x15`）の存在、IMU packet header、payload offset等。 ([GitHub][1])

---

## 5. パケット仕様（共通）

### 5.1 ヘッダ種別

| 先頭2byte | 意味         |
| ------- | ---------- |
| `FF FC` | IMUデータパケット |
| `FF FD` | MCU応答/ACK  |
| `FF FE` | MCUコマンド    |

([GitHub][1])

### 5.2 パケット構造（64byteレポート内の論理構造）

リバースエンジニアリング資料に記載された構造（オフセットは先頭=0）。 ([GitHub][1])

| Offset | Size | 内容                    | エンディアン/備考                   |
| -----: | ---: | --------------------- | --------------------------- |
| `0x00` |    2 | Header                | `FF FC/FD/FE`               |
| `0x02` |    2 | CRC-16-CCITT          | **ビッグエンディアン** ([GitHub][1]) |
| `0x04` |    2 | Payload length        | **リトルエンディアン** ([GitHub][1]) |
| `0x06` |    4 | Timestamp（IMU packet） | IMUパケットで使用 ([GitHub][1])    |
| `0x0A` |    4 | Reserved（ゼロ）          | ([GitHub][1])               |
| `0x0E` |    2 | Command ID            | **リトルエンディアン** ([GitHub][1]) |
| `0x10` |    2 | Message counter       | **リトルエンディアン** ([GitHub][1]) |
| `0x12` |    N | Payload data          | コマンド/IMUデータ本体 ([GitHub][1]) |
| `last` |    1 | End marker            | `0x03` ([GitHub][1])        |

> **長さの考え方（実装ヒント）**
> `payload_len = u16le(bytes[0x04..0x05])` とすると、論理パケットの総長は概ね `total_len = 6 + payload_len`。
> CRCは「offset 0x04 から total_len-1 まで」に対して計算する実装になっています。 ([GitHub][1])

---

## 6. CRC仕様

* **CRC-16-CCITT**
* polynomial: `0x1021`
* initial value: `0xFFFF`
* 計算範囲：**offset `0x04` 以降**（ヘッダとCRC自身を除外） ([GitHub][1])

> JS実装（WebHID）では lookup table を生成して `calcCrc16(packet, 4, packetLen-4)` の形で計算しています。 ([GitHub][1])

---

## 7. IMUストリーム開始（MCUコマンド）

### 7.1 コマンド概要

IMUデータを受信開始するには、MCUインタフェースへ以下を送ります。 ([GitHub][1])

* **Command ID**: `0x0015`
* **Data**:

  * `0x01` = enable
  * `0x00` = disable ([GitHub][1])

### 7.2 MCUコマンドパケット生成（論理）

WebHID実装に基づく組み立て仕様（要点）。 ([GitHub][2])

* header: `FF FE`
* reserved（0x06〜0x0D）: 0 埋め
* command id: offset `0x0E`（LE）
* msg counter: offset `0x10`（LE、適当なインクリメントでOK）
* data: offset `0x12` 以降
* end marker: `0x03`
* CRC: offset `0x02`（BE）。計算は offset `0x04` から end marker まで。 ([GitHub][2])

---

## 8. IMUデータ（姿勢）パケット

### 8.1 IMUパケット判定

入力レポート（64byte）の先頭が以下なら IMU packet として扱う。 ([GitHub][1])

* `bytes[0] == 0xFF && bytes[1] == 0xFC`

### 8.2 オイラー角の配置（payload offset）

IMUパケットの **payload offset は `0x12`（=18）**で、ここからオイラー角が格納されています。 ([GitHub][1])

* `raw0`：`bytes[18..21]`
* `raw1`：`bytes[22..25]`
* `raw2`：`bytes[26..29]` ([GitHub][1])

### 8.3 浮動小数点のエンディアン（重要）

リバースエンジニアリング資料では、これらは **IEEE754 float** で、**バイトスワップして解釈**する例が提示されています。 ([GitHub][1])

* 実装上は「4バイトを逆順にして little-endian float として読む」＝「big-endian float を読む」のと同義です（JS実装がその方式）。 ([GitHub][2])

### 8.4 軸マッピング（注意：実機で要検証）

WebHID実装では、取得した `raw0/raw1/raw2` をそのまま roll/pitch/yaw とせず、符号反転・入替をしています（“WebXR向け”調整）。 ([GitHub][2])

* 例（JS実装のコメント/処理より）

  * `yaw = -raw0`
  * `roll = -raw1`
  * `pitch = raw2` ([GitHub][2])

> **推奨**：あなたのアプリ側の座標系（右手/左手、Yaw軸の定義等）と、
> VITUREから来る角度の軸が一致する保証はありません。
> “右を向く/上を向く/右に傾ける”の3動作で、どのrawがどの軸かをログで確認してマッピングを確定してください（ここが一番ハマりやすい）。
> 仕様上の根拠は上の実装・ドキュメントですが、機種/ファームで差異が出うる点は留意。 ([GitHub][2])

---

## 9. 姿勢の表現：Euler → Quaternion（推奨）

多くの3Dエンジン/姿勢融合ではクォータニオンが扱いやすいので、Euler→Quaternion 変換を推奨します。

WebHID実装例（度→radして合成）に倣うなら、以下の形式です。 ([GitHub][1])

* 入力：roll/pitch/yaw（**degree**）
* 出力：`{w, x, y, z}`

> 注意：回転順序（Yaw-Pitch-Roll をどう適用するか）はエンジンにより流儀が違います。
> “見た目が合う順序”を実機で合わせてください（WebHID例は一つの実装です）。 ([GitHub][1])

---

## 10. リセンタ（Recenter）／キャリブレーション

「今向いている方向を正面(0)にする」用途には、**現在クォータニオンをオフセットとして保存**し、以後の姿勢に共役を掛ける方式が紹介されています。 ([GitHub][1])

* `offset = current_q`
* `q_calibrated = conj(offset) * q_raw` ([GitHub][1])

---

## 11. hidapi実装ガイド（Windows）

### 11.1 列挙と open（基本）

1. `hid_init()`
2. `hid_enumerate(0x35CA, 0x1131)`
3. 列挙された **全 path を open**（2つ以上見える想定）
4. 各デバイスで

   * `hid_set_nonblocking(dev, 1)`（推奨）
   * 受信スレッド/ループ開始

> もし列挙数が1しかない場合：
> Windows側で何かが掴んでいる（SpaceWalker等）/ドライバ状態/権限/接続形態（ハブ）などが疑わしいです。
> VITURE自身のツールが「他アプリで使用中だと接続前に止めてね」と注意しているので、まずSpaceWalker等を終了して試してください。 ([Viture][3])

### 11.2 Output Report の送り方（Report ID 0）

Report ID が `0x00` の場合でも、hidapiでは **先頭1byteに Report ID を付ける**流儀が一般的です（Windows実装で特に重要）。

* 送信バッファ例：`[0x00][packet bytes...]`

  * `0x00` は report id
  * `packet bytes` は 先頭が `FF FE` から始まるMCUコマンド本体

※ 送信長について：

* 仕様資料では report size は64byteですが、JS実装は短いパケット長でも `sendReport(0x00, packet)` しています。
  hidapi側では **64byteまでゼロパディング**し、CRC/length/end marker は論理パケット長に基づく…のが安全策です（余剰は無視される想定）。 ([GitHub][1])

### 11.3 受信（Input Report）

* `hid_read_timeout(dev, buf, 64, timeout_ms)` などで64byte受信
* `buf[0..1]` が `FF FC` なら IMU data
* `buf[0..1]` が `FF FD` なら MCU ACK（必要ならログ）
* CRCやlengthで妥当性チェックすると堅牢 ([GitHub][1])

---

## 12. 参考実装（擬似コード）

### 12.1 C風（概略）

```c
// 依存: hidapi
// 目的: VITURE Luma (VID 0x35CA, PID 0x1131) のIMUを有効化してIMU packetを読む

#define VID 0x35CA
#define PID 0x1131

// CRC-16-CCITT(0x1021, init 0xFFFF) を実装すること（本資料 6章）
// build_mcu_cmd_0x15_enable() で FF FE のコマンドパケットを作る（本資料 7章）

int main() {
  hid_init();

  // enumerate
  struct hid_device_info* devs = hid_enumerate(VID, PID);

  // open all
  std::vector<hid_device*> handles;
  for (auto* cur = devs; cur; cur = cur->next) {
    hid_device* h = hid_open_path(cur->path);
    if (h) {
      hid_set_nonblocking(h, 1);
      handles.push_back(h);
    }
  }
  hid_free_enumeration(devs);

  // build IMU enable cmd
  uint8_t cmd_payload[1] = { 0x01 }; // enable
  uint8_t cmd_packet[64] = {0};      // report body (64)
  size_t cmd_len = build_mcu_cmd_packet(cmd_packet, /*cmdId=*/0x0015, cmd_payload, 1);
  // cmd_len: 論理パケット長（例: 20）。reportは64送ってもよい。

  // send to all (MCU IF だけ受理する想定)
  for (auto* h : handles) {
    uint8_t out[65] = {0}; // [reportId=0] + 64 bytes
    out[0] = 0x00;
    memcpy(out + 1, cmd_packet, 64);
    hid_write(h, out, sizeof(out));
  }

  // read loop
  while (1) {
    for (auto* h : handles) {
      uint8_t in[64];
      int n = hid_read(h, in, sizeof(in));
      if (n == 64) {
        if (in[0] == 0xFF && in[1] == 0xFC) {
          // IMU packet -> parse Euler at offset 0x12
          // raw0/raw1/raw2: bytes[18..29], float32 big-endian（byte-swapして読む）
          // axis mapping は実機で合わせる（本資料 8.4）
        }
      }
    }
    // sleep a bit
  }
}
```

> 上記は「何をするべきか」の骨格です。
> CRC計算や float デコード、軸マッピングは本資料の該当章に従って実装してください。 ([GitHub][1])

---

## 13. 既知コマンド（現時点で確度が高いもの）

現状、公開されているリバースエンジニアリング資料で **明示されているコマンド**は以下です。 ([GitHub][1])

| Command ID | 意味         | Data                           |
| ---------: | ---------- | ------------------------------ |
|   `0x0015` | IMU on/off | `0x01` enable / `0x00` disable |

---

## 14. “SDKを使わない”方針のリスクと代替

* VITUREは **Linux向けにIMUアクセスAPIを提供するSDK**を公開しています（ただしWindows向けの同等低レベル仕様公開ではない）。 ([first.viture.com][4])
* Unity向けには VitureXR API（SDK）があり、モデル取得などのAPI体系が公開されています（ただしあなたの要望通り「Unityを噛ませない」用途には重い）。 ([VITURE Developer][5])
* WindowsではSpaceWalkerがヘッドトラッキング等を提供し、また一部のVITURE公式ツールは「Windowsでは先にSpaceWalker（ドライバ同梱）を入れる」旨の注意を出しています。環境によっては競合/排他が起きる可能性があるため、hidapiで直接叩く場合は SpaceWalker を終了して試験するのが安全です。 ([Viture][3])

---

## 15. 出典（一次情報・根拠）

* bfvogel / viture-webxr-extension

  * `VITURE_PROTOCOL.md`（WebHID向けに整理されたプロトコル仕様：VID/PID、2IF構成、パケット構造、CRC、IMU enable、IMUデータ位置） ([GitHub][1])
  * `viture-hid.js`（実装：コマンド組み立て、CRC計算、IMU packet判定、payload offset、floatデコード、軸マッピング） ([GitHub][2])
* VITURE公式：VITURE XR Glasses SDK for Linux（IMUデータアクセスSDKの存在） ([first.viture.com][4])
* VITURE公式：Firmware Update / DFUツールの注意（WindowsでSpaceWalker/ドライバ同梱に言及） ([Viture][3])
* VITURE公式（Academy）：SpaceWalker for Windows（3DoF等の機能説明） ([Viture Academy][6])


[1]: https://raw.githubusercontent.com/bfvogel/viture-webxr-extension/main/VITURE_PROTOCOL.md "https://raw.githubusercontent.com/bfvogel/viture-webxr-extension/main/VITURE_PROTOCOL.md"
[2]: https://raw.githubusercontent.com/bfvogel/viture-webxr-extension/main/viture-hid.js "https://raw.githubusercontent.com/bfvogel/viture-webxr-extension/main/viture-hid.js"
[3]: https://www.viture.com/firmware/update "https://www.viture.com/firmware/update"
[4]: https://first.viture.com/developer/viture-sdk-for-linux "https://first.viture.com/developer/viture-sdk-for-linux"
[5]: https://developer.viture.com/unity/viturexr_api "https://developer.viture.com/unity/viturexr_api"
[6]: https://academy.viture.com/xr_glasses/spacewalker_windows "https://academy.viture.com/xr_glasses/spacewalker_windows"
