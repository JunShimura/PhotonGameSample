# PhotonGameSample プロジェクト概要

このプロジェクトは、UnityとPhoton Fusionを使用して構築されたマルチプレイヤーゲームのサンプルです。ゲームの基本的な流れ、主要なスクリプトの役割、およびイベントシステムについて解説します。

## 📚 目次
- [アーキテクチャの改善](#-アーキテクチャの改善)
- [ゲームの流れ](#1-ゲームの開始から終了までの流れ)
- [ソースコードの関係性](#2-各ソースコードの関係性)
- [イベントシステム](#3-イベントシステムの仕組み-gameeventscs)
- [各ソースコードの詳細](#4-各ソースコードの内容)
- [Prefabsディレクトリ](#prefabs-ディレクトリ概要)
- [責任分離の改善](#6-アーキテクチャ改善playeravatarの責任分離)
- [段階的改造ガイド](#7-段階的な改造拡張ポイント)

## ⚡ アーキテクチャの改善
このプロジェクトでは、コードの保守性と拡張性を向上させるため、ゲーム進行管理機能をPlayerAvatarから分離し、専用の`GameSyncManager`を導入しています。これにより、各クラスの責任が明確化され、より良いアーキテクチャを実現しています。

## 1. ゲームの開始から終了までの流れ

1.  **アプリケーション起動と接続:**
    *   `GameLauncher.cs`: アプリケーション起動時にPhoton Fusionの`NetworkRunner`を初期化し、Photon Cloudへの接続を開始します。`GameMode.Shared`でセッションを開始し、プレイヤーが参加できる状態にします。

2.  **プレイヤーの参加とスポーン:**
    *   `GameLauncher.cs` (`OnPlayerJoined`コールバック): 新しいプレイヤーがセッションに参加すると、このコールバックが呼び出されます。
    *   `GameController.cs` (`OnPlayerSpawned`): `NetworkGameManager`を通じてプレイヤーのアバターがネットワーク上にスポーンされると、`GameController`がその通知を受け取ります。スポーンされたプレイヤーは`PlayerManager`に登録されます。

3.  **ゲーム開始条件のチェック:**
    *   `GameController.cs` (`CheckPlayerCountAndUpdateGameState`): プレイヤーが参加するたびに、現在のプレイヤー数をチェックします。`MAX_PLAYERS`（デフォルト2人）に達すると、ゲームの状態が`WaitingForPlayers`から`InGame`に遷移し、ゲームが開始されます。
    *   プレイヤーの入力が有効化され、ゲームプレイが可能になります。

4.  **ゲームプレイ:**
    *   プレイヤーはアイテムを収集します。アイテムの収集状況は`ItemManager.cs`によって管理されます。
    *   プレイヤーのスコアは`PlayerAvatar.cs`内で管理され、変更されると`GameEvents.OnPlayerScoreChanged`イベントを通じて通知されます。

5.  **ゲーム終了条件と勝者決定:**
    *   `GameRuleProcessor.cs`: 全てのアイテムが収集されると、`ItemManager`から`OnAllItemsCollected`イベントが発火し、`GameRuleProcessor`がゲーム終了をトリガーします。
    *   `GameController.cs` (`EndGame`): `GameRuleProcessor`からの通知を受けて、`GameController`がゲームを終了状態に遷移させ、全プレイヤーの入力を無効化します。
    *   `GameRuleProcessor.cs` (`DetermineWinner`): スコア更新が完了した後、`PlayerManager`からプレイヤーのスコア情報を取得し、勝者を決定します。引き分けの場合も考慮されます。
    *   勝者決定の結果は`GameEvents.OnWinnerDetermined`イベントを通じて通知され、UIに表示されます。

6.  **ゲームのリスタート（オプション）:**
    *   ゲーム終了後、一定時間経過するとゲームがリスタートするロジックが`GameController.cs`に実装されています（`RestartGameAfterDelay`）。

## 2. 各ソースコードの関係性

主要なスクリプトとその関係性は以下の通りです。

*   **`GameLauncher.cs`**: Photon Fusionのセッション管理とプレイヤーの接続を担当します。`NetworkRunner`のライフサイクルイベントを処理し、`GameController`にプレイヤーの参加やスポーンを通知します。
*   **`GameController.cs`**: ゲーム全体の状態（待機中、ゲーム中、ゲーム終了）を管理する中心的なスクリプトです。`PlayerManager`, `ItemManager`, `NetworkGameManager`, `GameSyncManager`, `GameUIManager`, `GameRuleProcessor`といった他のマネージャーコンポーネントと連携し、ゲームの進行を制御します。イベント駆動で各マネージャーと通信します。
*   **`PlayerManager.cs`**: ゲームに参加しているプレイヤーのアバター（`PlayerAvatar`）を管理します。プレイヤーの登録、登録解除、スコア変更、プレイヤー数変更などのイベントを発火します。`GameRuleProcessor`に勝者決定のための情報を提供します。
*   **`ItemManager.cs`**: ゲーム内のアイテムの生成、収集、および収集状況を管理します。全てのアイテムが収集された際に`GameRuleProcessor`に通知します。
*   **`GameRuleProcessor.cs`**: ゲームの終了条件（全アイテム収集など）を判定し、ゲームの勝者を決定するロジックを担います。`PlayerManager`からスコア情報を取得し、`GameEvents`を通じて勝者決定を通知します。
*   **`GameUIManager.cs`**: ゲームのUI表示を管理します。ゲームの状態変化やスコア更新などのイベントを`GameEvents`から受け取り、UIを更新します。
*   **`NetworkGameManager.cs`**: Photon Fusionのネットワーク同期に関する処理をラップし、ネットワーク関連のイベントを`GameController`などの他のコンポーネントに通知する役割を担います。`NetworkRunner`のコールバックを直接処理するのではなく、より高レベルなイベントとして提供します。
*   **`GameSyncManager.cs`**: ゲーム進行の同期管理を専門に担当するNetworkBehaviourクラスです。カウントダウン、ゲーム状態変更、再開処理、アイテムリセットなどのRPC通信を通じて、クライアント間のゲーム進行を同期します。PlayerAvatarから分離されたことで、ゲーム進行管理がより明確に責任分離されています。
*   **`PlayerAvatar.cs` (Prefabs配下)**: 各プレイヤーのネットワークオブジェクトであり、プレイヤー個別の機能（移動、アニメーション、スコア、ニックネーム）に特化しています。ゲーム進行関連のRPCは`GameSyncManager`に移譲され、プレイヤー固有の機能のみを担当するよう簡素化されています。
*   **`Item.cs` (Prefabs配下)**: ゲーム内の収集可能なアイテムの挙動を定義します。プレイヤーがアイテムに触れた際の処理（収集）を管理します。

## 3. イベントシステムの仕組み (`GameEvents.cs`)

`GameEvents.cs`は、ゲーム内の様々なイベントを一元的に管理するための静的クラスです。Unityのイベントシステム（`Action`デリゲート）を利用して、各コンポーネント間の疎結合な通信を実現しています。

*   **イベントの定義:** `public static event Action<T> EventName;` の形式でイベントが定義されています。例えば、`OnGameStateChanged`はゲームの状態が変更されたときに発火します。
*   **イベントの発火:** `TriggerEventName(args);` の形式で、対応するイベントが発火されます。例えば、`GameEvents.TriggerGameStateChanged(newState);` を呼び出すことで、`OnGameStateChanged`イベントを購読している全てのリスナーに通知が送られます。
*   **イベントの購読:** 各コンポーネントは、`GameEvents.EventName += YourMethod;` の形式でイベントを購読し、イベント発生時に`YourMethod`が呼び出されるように設定します。これにより、イベントの発火元と購読元が直接依存することなく、柔軟なシステム構築が可能になります。

**主なイベント:**
*   `OnGameStateChanged`: ゲームの状態（待機中、カウントダウン中、ゲーム中、ゲーム終了）が変更された時。
*   `OnPlayerScoreChanged`: プレイヤーのスコアが変更された時。
*   `OnWinnerDetermined`: ゲームの勝者が決定された時。
*   `OnPlayerCountChanged`: ゲーム内のプレイヤー数が変更された時。
*   `OnPlayerRegistered`: 新しいプレイヤーが登録された時。
*   `OnGameEnd`: ゲームが終了した時。
*   `OnScoreUpdateCompleted`: スコアの更新が完了した時。
*   `OnCountdownUpdate`: ゲーム開始カウントダウンの更新時。

## 4. 各ソースコードの内容

### `GameController.cs`

*   **役割**: ゲームの全体的な進行と状態を管理します。他のマネージャー（`ItemManager`, `PlayerManager`など）と連携し、ゲームの開始、終了、プレイヤーの参加/離脱、スコア更新などを調整します。
*   **主要な機能**: 
    *   ゲーム状態の管理 (`CurrentGameState`プロパティ)。
    *   各マネージャーコンポーネントの参照とイベント購読/解除。
    *   プレイヤー数のチェックとゲーム状態の更新 (`CheckPlayerCountAndUpdateGameState`)。
    *   ゲーム終了処理 (`EndGame`)。
    *   プレイヤーの入力有効/無効化。
    *   勝者決定結果の受け取りとブロードキャスト。

### `GameEvents.cs`

*   **役割**: ゲーム全体で利用されるイベントを定義し、その発火メソッドを提供します。各コンポーネント間の疎結合な通信を促進します。
*   **主要な機能**: 
    *   ゲームの状態変化、プレイヤーのスコア変化、勝者決定など、様々なイベントの定義。
    *   各イベントを発火させるための静的メソッド (`Trigger...` メソッド)。

### `GameLauncher.cs`

*   **役割**: Photon Fusionのネットワークセッションを初期化し、プレイヤーの接続を管理します。`INetworkRunnerCallbacks`インターフェースを実装し、Photon Fusionからのネットワークイベントを処理します。
*   **主要な機能**: 
    *   `NetworkRunner`プレハブのインスタンス化と初期化。
    *   Photon Cloudへの接続とゲームセッションの開始 (`StartGame`)。
    *   プレイヤーの参加 (`OnPlayerJoined`)、離脱 (`OnPlayerLeft`)、接続失敗 (`OnConnectFailed`)などのネットワークイベントのハンドリング。
    *   `GameController`へのプレイヤー参加通知。

### `GameRuleProcessor.cs`

*   **役割**: ゲームの終了条件を判定し、ゲームの勝者を決定するロジックを実装します。主に`ItemManager`からの全アイテム収集イベントや、`GameController`からのゲーム終了要求を受けて動作します。
*   **主要な機能**: 
    *   ゲーム終了のトリガー (`TriggerGameEndByRule`)。
    *   スコア更新の完了を待機し、タイムアウト処理を行うコルーチン。
    *   `PlayerManager`からスコア情報を取得し、勝者（または引き分け）を決定する (`DetermineWinner`)。
    *   決定された勝者メッセージを`GameEvents`を通じてブロードキャスト。

### `GameUIManager.cs`

*   **役割**: ゲームのユーザーインターフェース（UI）の表示と更新を管理します。`GameEvents`からゲームの状態変化やスコア更新などの通知を受け取り、それに応じてUI要素を操作します。
*   **主要な機能**: 
    *   ゲームの状態表示（例: 「Waiting for Players...」、「Game Started!」）。
    *   プレイヤーのスコア表示の更新。
    *   勝者メッセージの表示。
    *   ゲーム終了時のUI要素の切り替え。

### `ItemManager.cs`

*   **役割**: ゲーム内に存在する収集可能なアイテムの管理を行います。アイテムの生成、プレイヤーによる収集、および収集状況の追跡を担当します。
*   **主要な機能**: 
    *   ゲーム開始時のアイテムの初期化と配置。
    *   プレイヤーがアイテムを収集した際の処理。
    *   収集されたアイテム数のカウントと、全アイテム収集時のイベント発火 (`OnAllItemsCollected`)。
    *   ゲーム状態のリセット時のアイテム状態のリセット。

### `NetworkGameManager.cs`

*   **役割**: Photon Fusionの`NetworkRunner`をラップし、ネットワーク関連のイベントを`GameController`などの他のコンポーネントに通知する役割を担います。`NetworkRunner`のコールバックを直接処理するのではなく、より高レベルなイベントとして提供します。
*   **主要な機能**: 
    *   プレイヤーの参加、離脱、スポーンに関するイベントの提供。
    *   ゲーム終了要求の処理。
    *   `NetworkRunner`インスタンスへのアクセス提供。

### `GameSyncManager.cs`

*   **役割**: ゲーム進行の同期管理を専門に担当するNetworkBehaviourクラスです。PlayerAvatarから分離されたゲーム進行関連のRPC機能を一元管理し、クライアント間でのゲーム状態同期を実現します。
*   **主要な機能**: 
    *   ゲーム再開クリックの同期（`NotifyRestartClick`）
    *   カウントダウン更新の同期（`NotifyCountdownUpdate`）
    *   ゲーム状態変更の同期（`NotifyGameStateChanged`）
    *   プレイヤー入力制御の同期（`NotifyEnableAllPlayersInput`）
    *   ゲーム再開処理の同期（`NotifyGameRestart`）
    *   アイテムリセットの同期（`NotifyItemsReset`）
*   **アーキテクチャ上の利点**: 
    *   PlayerAvatarがプレイヤー個別機能に集中できるよう責任を分離
    *   ゲーム進行管理の一元化により保守性が向上
    *   新しいゲーム進行機能の追加時、既存のPlayerAvatarに影響しない

### `PlayerManager.cs`

*   **役割**: ゲームに参加している全てのプレイヤーアバター（`PlayerAvatar`）の情報を一元的に管理します。プレイヤーの登録、登録解除、スコアの追跡、および勝者決定のための情報提供を行います。
*   **主要な機能**: 
    *   `PlayerAvatar`の登録と登録解除。
    *   各プレイヤーのスコアの管理と更新。
    *   プレイヤー数の追跡。
    *   最も高いスコアを持つプレイヤー（勝者）を特定するロジック (`DetermineWinner`)。
    *   プレイヤーの入力状態の有効/無効化。



# Prefabs ディレクトリ概要

このディレクトリには、ゲーム内で使用される主要なプレハブとその関連スクリプトが含まれています。主にネットワーク同期されるオブジェクトや、ゲームプレイに直接関わる要素が定義されています。

## 1. 主要なプレハブとスクリプト

### `Item.prefab` と `Item.cs`

*   **役割**: ゲーム内でプレイヤーが収集するアイテムのプレハブと、その挙動を制御するスクリプトです。
*   **`Item.cs` の内容**: 
    *   アイテムが収集された際の処理（例: プレイヤーのスコア加算、アイテムの非アクティブ化）。
    *   プレイヤーがアイテムに触れたことを検出するトリガーロジック。
    *   ネットワークを介したアイテムの状態同期（収集済みかどうかなど）。

### `PlayerAvatar.prefab` と `PlayerAvatar.cs`

*   **役割**: ゲームに参加する各プレイヤーを表すアバターのプレハブと、その挙動を制御するスクリプトです。アーキテクチャ改善により、プレイヤー個別の機能に特化し、ゲーム進行管理は`GameSyncManager`に分離されています。
*   **`PlayerAvatar.cs` の内容**: 
    *   プレイヤーの移動、回転、ジャンプの制御
    *   プレイヤーのスコア、ニックネーム、IDなどの情報の管理とネットワーク同期
    *   アイテム取得とスコア更新のRPC処理
    *   勝者メッセージRPC（プレイヤー固有の通知機能）
    *   プレイヤーの入力処理（移動、ジャンプなど）
    *   `NetworkBehaviour`を継承し、Photon Fusionによるネットワーク同期を実現
*   **改善点**: 
    *   ゲーム進行関連のRPC（カウントダウン、状態変更、再開処理など）を`GameSyncManager`に移譲
    *   プレイヤー固有の責任に集中することで、コードの可読性と保守性が向上

### `NetworkRunner.prefab`

*   **役割**: Photon Fusionのネットワークセッションを管理するためのコアコンポーネントである`NetworkRunner`のプレハブです。`GameLauncher.cs`によってインスタンス化され、ゲームのネットワーク通信の基盤となります。
*   **内容**: 
    *   Photon Fusionのネットワークセッションの開始、参加、終了。
    *   ネットワークオブジェクトのスポーンとデスポーン。
    *   ネットワーク上のデータ同期とイベント処理。

## 2. Prefabsディレクトリ内のその他のファイル

*   **`.mat` ファイル**: 各プレハブに適用されるマテリアルファイルです。オブジェクトの見た目を定義します。
*   **`.meta` ファイル**: Unityが内部的に使用するメタデータファイルです。各アセットの設定情報などが含まれています。

このディレクトリのファイルは、ゲームの実行時に動的に生成またはロードされるオブジェクトのテンプレートとして機能し、マルチプレイヤー環境でのゲームプレイを支える重要な要素です。



### `ItemCatcher.cs`

*   **役割**: プレイヤーがアイテムを「キャッチ」したことを検出・処理するスクリプトです。主に`PlayerAvatar`にアタッチされ、アイテムとの衝突イベントを処理します。
*   **内容**: 
    *   `OnItemCaught`イベントを定義し、アイテムがキャッチされた際に通知します。
    *   `ItemCought`メソッドは、アイテムがキャッチされたときに呼び出され、関連するイベントを発火させます。

### `PlayerAvatarAnimationEventReceiver.cs`

*   **役割**: プレイヤーアバターのアニメーションイベントを受け取り、それに応じたサウンドエフェクト（足音、着地音など）を再生するスクリプトです。
*   **内容**: 
    *   アニメーションクリップに設定されたイベント（例: `OnFootstep`, `OnLand`）に対応するメソッドを実装します。
    *   指定されたオーディオクリップを再生します。

### `PlayerAvatarView.cs`

*   **役割**: プレイヤーアバターの視覚的な表現（モデル、マテリアルなど）を管理し、ネットワーク同期されたデータに基づいてアバターの外観を更新するスクリプトです。
*   **内容**: 
    *   プレイヤーのニックネームやスコアをUIに表示する処理。
    *   プレイヤーのモデルやマテリアルの切り替え。
    *   ネットワーク同期されたデータ（例: `PlayerAvatar.cs`からのデータ）を視覚的に反映。

### `PlayerModel.cs`

*   **役割**: プレイヤーの基本的なデータ構造と、そのデータを操作するためのロジックを定義するスクリプトです。主にプレイヤーの統計情報や状態を保持します。
*   **内容**: 
    *   プレイヤーID、ニックネーム、スコアなどのプロパティ。
    *   プレイヤーの状態（例: 生存、死亡）を管理するロジック。
    *   スコアの加算や減算などのデータ操作メソッド。




## 5. ゲームイベントの流れと`GameEvents.cs`の発火順序

ゲーム内の主要なイベントは`GameEvents.cs`を通じて管理され、各コンポーネント間の疎結合な通信を実現しています。以下に、ゲーム開始からアイテム取得、スコア表示、勝敗判定の表示までのイベントの流れと、`GameEvents.cs`での発火および発火するクラスの順序を解説します。

### 5.1. ゲーム開始

1.  **`GameLauncher.cs`**: Photon Cloudへの接続が成功し、セッションが開始されると、`GameLauncher.cs`は`NetworkRunner`のコールバックを通じてプレイヤーの参加を検知します。
2.  **`GameController.cs`**: `GameLauncher.cs`からの通知を受け、`GameController.cs`はプレイヤー数をチェックします。`MAX_PLAYERS`に達すると、ゲームの状態を`WaitingForPlayers`から`CountdownToStart`に遷移させ、5秒のカウントダウンを開始します。
    *   **`GameEvents.TriggerGameStateChanged(GameState.CountdownToStart)`**: `GameController.cs`がゲーム状態の変更を`GameEvents`を通じて発火します。
    *   **`GameEvents.TriggerCountdownUpdate(remainingSeconds)`**: `GameController.cs`が1秒ごとにカウントダウンの残り時間を`GameEvents`を通じて発火します。これにより、`GameUIManager.cs`がカウントダウン表示を更新します。
3.  **ゲーム開始**: カウントダウンが完了すると、ゲームの状態が`InGame`に遷移し、全プレイヤーの操作が有効化されます。
    *   **`GameEvents.TriggerGameStateChanged(GameState.InGame)`**: `GameController.cs`がゲーム開始を`GameEvents`を通じて発火します。これにより、`GameUIManager.cs`などがゲーム開始UIを更新します。

### 5.2. アイテム取得とスコア表示

1.  **`Item.cs`**: プレイヤーがゲーム内のアイテムに触れると、`Item.cs`内のロジックがアイテムの収集を検知します。
2.  **`ItemCatcher.cs`**: `Item.cs`からの通知を受け、`ItemCatcher.cs`（`PlayerAvatar`にアタッチされている）がアイテムのキャッチを処理します。
3.  **`PlayerAvatar.cs`**: `ItemCatcher.cs`からの通知を受け、`PlayerAvatar.cs`は自身のスコアを更新します。このスコア更新はネットワーク同期されます。
    *   **`GameEvents.TriggerPlayerScoreChanged(playerId, newScore)`**: `PlayerAvatar.cs`が自身のスコア変更を`GameEvents`を通じて発火します。これにより、`GameUIManager.cs`などがプレイヤーのスコア表示をリアルタイムで更新します。
4.  **`ItemManager.cs`**: `Item.cs`が収集されると、`ItemManager.cs`は収集されたアイテム数を追跡します。

### 5.3. 勝敗判定の表示

1.  **`ItemManager.cs`**: 全てのアイテムが収集されると、`ItemManager.cs`は`OnAllItemsCollected`イベントを発火します。
2.  **`GameRuleProcessor.cs`**: `ItemManager.cs`からの`OnAllItemsCollected`イベントを受け取り、`GameRuleProcessor.cs`はゲーム終了のトリガーと判断します。
    *   **`GameEvents.TriggerGameEnd()`**: `GameRuleProcessor.cs`がゲーム終了を`GameEvents`を通じて発火します。これにより、`GameController.cs`がゲームを終了状態に遷移させ、プレイヤーの入力を無効化します。
3.  **`GameRuleProcessor.cs`**: ゲーム終了後、`GameRuleProcessor.cs`は`PlayerManager.cs`から最終的なスコア情報を取得し、勝者を決定します。
    *   **`GameEvents.TriggerWinnerDetermined(winnerId, winnerName, winnerScore)`**: `GameRuleProcessor.cs`が勝者決定の結果を`GameEvents`を通じて発火します。これにより、`GameUIManager.cs`などが勝者メッセージをUIに表示します。

このイベント駆動の仕組みにより、各コンポーネントは互いに直接依存することなく、柔軟かつ拡張性の高いゲームロジックを実現しています。

## 6. アーキテクチャ改善：PlayerAvatarの責任分離

### 6.1. 改善の背景
初期実装では`PlayerAvatar.cs`にプレイヤー個別の機能とゲーム進行管理の機能が混在しており、以下の問題がありました：
- 単一責任原則の違反（プレイヤー機能 + ゲーム進行管理）
- コードの可読性低下（400行超の肥大化）
- 保守性の低下（変更時の影響範囲が不明確）
- テスタビリティの低下（機能が密結合）

### 6.2. 解決策：GameSyncManagerの導入
`GameSyncManager`を新規作成し、以下のゲーム進行管理機能をPlayerAvatarから分離：

#### 分離された機能（6種類のRPCメソッド）
- `NotifyRestartClick` / `RPC_NotifyRestartClick` - 再開クリックの同期
- `NotifyCountdownUpdate` / `RPC_NotifyCountdownUpdate` - カウントダウンの同期  
- `NotifyGameStateChanged` / `RPC_NotifyGameStateChanged` - ゲーム状態変更の同期
- `NotifyEnableAllPlayersInput` / `RPC_NotifyEnableAllPlayersInput` - 入力制御の同期
- `NotifyGameRestart` / `RPC_NotifyGameRestart` - ゲーム再開処理の同期
- `NotifyItemsReset` / `RPC_NotifyItemsReset` - アイテムリセットの同期

#### PlayerAvatarに残された機能
- プレイヤーの移動・ジャンプ制御
- スコア管理（OnItemCaught、RPC_UpdateScore）
- プレイヤー固有の状態管理
- 勝者メッセージRPC（プレイヤー固有の機能）

### 6.3. 改善効果

| 改善項目 | 改善前 | 改善後 |
|----------|--------|--------|
| **責任の分離** | PlayerAvatarが複数責任 | 各クラスが単一責任 |
| **コード行数** | PlayerAvatar: 400行超 | PlayerAvatar: 300行程度<br>GameSyncManager: 120行程度 |
| **保守性** | 変更時の影響範囲が不明確 | 機能別に明確に分離 |
| **テスタビリティ** | 機能が密結合でテスト困難 | 独立してテスト可能 |
| **拡張性** | 新機能追加時に既存コード影響 | 適切なクラスに機能追加可能 |

### 6.4. 実装パターン
```csharp
// 改善前：PlayerAvatar内でゲーム進行RPC
public class PlayerAvatar : NetworkBehaviour 
{
    // プレイヤー機能 + ゲーム進行管理が混在
    public void NotifyRestartClick() { ... }
    public void NotifyCountdownUpdate() { ... }
    // ... 他のゲーム進行RPC
}

// 改善後：責任の明確な分離
public class PlayerAvatar : NetworkBehaviour 
{
    // プレイヤー個別機能のみに集中
    private void OnItemCaught() { ... }
    public void ResetScore() { ... }
}

public class GameSyncManager : NetworkBehaviour 
{
    // ゲーム進行同期管理に特化
    public void NotifyRestartClick() { ... }
    public void NotifyCountdownUpdate() { ... }
}
```

この改善により、より保守しやすく拡張可能なアーキテクチャを実現しています。

## 7. 段階的な改造・拡張ポイント

このゲームを段階的に改造・拡張する際の推奨ポイントを、難易度と必要な知識レベル別に整理しました。

### 7.1. 初級レベル（Unity基礎知識）

#### A. UIとビジュアル改善
- **プレイヤーアバターの見た目変更**
  - `PlayerAvatar.prefab`のモデルやマテリアルを変更
  - `PlayerAvatarView.cs`でプレイヤー別の色分けやスキン追加
  - 対象ファイル: `Prefabs/PlayerAvatar.prefab`, `PlayerAvatarView.cs`

- **アイテムの種類拡張**
  - `Item.prefab`を複製して異なるスコアを持つアイテム作成
  - `Item.cs`でアイテム種別プロパティ追加
  - 対象ファイル: `Prefabs/Item.prefab`, `Item.cs`, `ItemManager.cs`

- **UI表示の改善**
  - `GameUIManager.cs`でスコア表示、タイマー表示の改善
  - ゲーム状態に応じたUI要素の追加
  - 対象ファイル: `GameUIManager.cs`

#### B. ゲームルールの簡単な変更
- **勝利条件の変更**
  - `GameRuleProcessor.cs`で時間制限やスコア閾値による勝利条件追加
  - 対象ファイル: `GameRuleProcessor.cs`

- **プレイヤー数の変更**
  - `GameController.cs`の`MAX_PLAYERS`定数変更
  - 対象ファイル: `GameController.cs`

### 7.2. 中級レベル（ネットワーク基礎知識）

#### A. 新しいゲーム機能追加
- **パワーアップアイテム実装**
  - 移動速度アップ、ジャンプ力アップなどの一時的効果
  - `PlayerAvatar.cs`に状態効果システム追加
  - `GameSyncManager.cs`でパワーアップ状態同期
  - 対象ファイル: `PlayerAvatar.cs`, `GameSyncManager.cs`, `Item.cs`

- **エリア制限・障害物追加**
  - マップにコライダーで境界設定
  - `PlayerAvatar.cs`の移動制限ロジック追加
  - 対象ファイル: `PlayerAvatar.cs`, シーン設定

- **チーム戦モード実装**
  - `PlayerModel.cs`にチーム情報追加
  - `GameRuleProcessor.cs`でチーム別勝利判定
  - `PlayerManager.cs`でチーム管理機能
  - 対象ファイル: `PlayerModel.cs`, `GameRuleProcessor.cs`, `PlayerManager.cs`

#### B. ゲーム進行システム拡張
- **ラウンド制ゲーム実装**
  - `GameController.cs`にラウンド管理機能追加
  - `GameSyncManager.cs`でラウンド状態同期RPC追加
  - 対象ファイル: `GameController.cs`, `GameSyncManager.cs`, `GameEvents.cs`

- **観戦モード実装**
  - `GameLauncher.cs`で観戦者とプレイヤーの区別
  - `NetworkGameManager.cs`で観戦者用の接続処理
  - 対象ファイル: `GameLauncher.cs`, `NetworkGameManager.cs`

### 7.3. 上級レベル（高度なネットワーク知識）

#### A. パフォーマンス最適化
- **ネットワーク通信最適化**
  - `PlayerAvatar.cs`のNetworkProperty使用量最適化
  - `GameSyncManager.cs`のRPC送信頻度制御
  - Fusion Tickの最適化
  - 対象ファイル: `PlayerAvatar.cs`, `GameSyncManager.cs`

- **サーバー権限管理強化**
  - `GameController.cs`でチート防止ロジック強化
  - `ItemManager.cs`でサーバー側でのアイテム管理
  - 対象ファイル: `GameController.cs`, `ItemManager.cs`, `PlayerAvatar.cs`

#### B. 複雑なゲームモード
- **バトルロワイヤルモード**
  - マップの段階的縮小システム
  - 生存者管理とエリミネーション
  - `GameRuleProcessor.cs`で複雑な勝利条件
  - 新規クラス: `BattleRoyaleManager.cs`, `MapShrinkManager.cs`

- **アビリティシステム実装**
  - プレイヤー固有の特殊能力
  - クールダウン管理とネットワーク同期
  - `PlayerAvatar.cs`大幅拡張または新規`AbilityManager.cs`作成

### 7.4. 専門レベル（ゲーム開発全般知識）

#### A. AIシステム統合
- **NPCプレイヤー実装**
  - `PlayerAvatar.cs`のAI制御版作成
  - `GameController.cs`でAIプレイヤー管理
  - NavMeshを使用した移動AI

- **マッチメイキングシステム**
  - Photon CloudのマッチメイキングAPI活用
  - `GameLauncher.cs`でスキルベースマッチング
  - レーティングシステム実装

#### B. データ永続化・解析
- **プレイヤー統計保存**
  - 外部データベース（Firebase等）との統合
  - `PlayerModel.cs`拡張でプレイヤー履歴管理
  - ゲーム結果の永続化

- **リアルタイム解析**
  - ゲームプレイデータ収集システム
  - `GameEvents.cs`拡張でイベント解析
  - パフォーマンスメトリクス収集

### 7.5. 改造時の推奨手順

1. **計画フェーズ**
   - 既存の`GameEvents.cs`で必要なイベント追加検討
   - 影響を受けるクラスの洗い出し
   - ネットワーク同期が必要な要素の特定

2. **実装フェーズ**
   - ローカル機能実装 → ネットワーク同期実装の順序
   - `GameSyncManager.cs`へのRPC追加（ゲーム進行関連）
   - `PlayerAvatar.cs`へのRPC追加（プレイヤー固有機能）

3. **テストフェーズ**
   - シングルプレイヤーでの機能テスト
   - マルチプレイヤーでの同期テスト
   - エッジケース（接続切断、再接続）のテスト

### 7.6. 改造時の注意点

- **アーキテクチャの維持**: 責任分離の原則を維持し、適切なクラスに機能追加
- **ネットワーク負荷**: RPC頻度とデータサイズの最適化
- **後方互換性**: 既存のセーブデータやネットワークプロトコルとの互換性
- **デバッグ**: `GameEvents.cs`のイベントログ活用でデバッグ効率化

この段階的なアプローチにより、学習曲線に沿った無理のないゲーム拡張が可能になります。

