# Scorpion 開発規約

このファイルは `CLAUDE.md`（Claude Code 用）と `AGENTS.md`（Codex 用）で同じ内容にする。変更するときは両方を更新すること。

## 規約の参照先

C# または Unity のコードを作成・修正するときは、作業開始時に次の Notion ページの最新版を取得して読み、実装規約として適用する。同じ作業中は取得済みの内容を再利用してよい。

1. プロジェクト固有規約（優先）：命名規則と提出方式（プログラマー）
   https://www.notion.so/421ed4d1f2c881a5ac14d731bc8ca6f6
2. 共通規約：Unity / C# コード規定
   https://www.notion.so/Unity-C-3f2ed4d1f2c881beb3c0e0b4e86d7aa6

- 1 と 2 が食い違う場合は 1 を優先する。機械的なフォーマットは `.editorconfig` を優先する。
- Notion を取得できない場合は、取得できなかったことを明示する。確認済みと偽らず、規約の内容が必要ならユーザーに本文の共有を依頼する。下の「要点」は Notion を読めないときの最低限の目安とする。

## 要点

- 命名：型・Property・Method は PascalCase、interface は `I` + PascalCase、private field は `_camelCase`、引数・ローカル変数は camelCase、const は UPPER_SNAKE_CASE。Event は `OnXxx`、Event Handler は `HandleXxx`、UI Toolkit の uxml / uss は lower-kebab-case。
- メンバーの順序：コンストラクタ → public event → public property → public enum / class / struct → public const → SerializeField → private field → private const → public method → Unity ライフサイクル → イベントハンドラ → protected / virtual → private method → private enum / class / struct → デバッグ用処理。既存ファイルを順序合わせのためだけに並び替えない。
- Summary・日本語コメント・`SerializeField` + `Tooltip` の付け方は共通規約に従う。
- 対象・呼び出し元・依存先・同種実装を確認し、依頼と無関係な改名・並び替え・整形・リファクタリングを行わない。
- `SerializeField` や公開 API の変更は、Prefab / Scene / ScriptableObject や呼び出し側への影響を確認する。

## Git・提出方式

- 作業は個人ブランチ（例：`feature/yamada`）で行い、`main` へ直接コミットしない。
- 提出は次の手順で行う。
  1. 変更をプッシュし、プルリクエスト（PR）を作る。
  2. PR 本文に、変更内容・確認したこと・未確認事項を簡潔に書く。
  3. PR 本文またはコメントで、リーダーをメンションしてレビューを依頼する。

## Unity の操作（MCP）

- Unity エディタは `.mcp.json` の `unity` サーバー経由で操作できる。
- シーン・Prefab・アセットを変更する前に、作業中の変更をコミットしておく。
- 同じエディタを複数のセッションから同時に操作しない。

## 完了報告

コード変更後は、変更内容、変更ファイル、確認内容、未確認事項・残る問題を簡潔に報告し、最後に「変更内容のレビューをお願いします」と記す。
