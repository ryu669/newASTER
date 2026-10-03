# 計画7：同梱日本語フォント

Noto Sans CJK JP Regular を未加工のOTFとして `Resources/Fonts` に同梱する。OSフォントの検索を廃止し、起動時に同梱フォントのロードと代表文字の存在を検査する。欠落時は明示的に失敗するため、検証が別のOSフォントへ無言で置き換わることはない。

出典は [公式配布ファイル](https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/Japanese/NotoSansCJKjp-Regular.otf)。SHA-256 は `68A3FC98800B2A27B371F2FB79991DAF3633BD89309D4FFAA6946FD587F375B5`。画像や音源の自作素材とは別の第三者素材として管理する。

[SIL Open Font License 1.1](https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE) の全文と著作権・出典通知を素材とともに保存し、Windowsビルドにも `ThirdPartyNotices/NotoSansCJKjp` としてコピーする。フォントの著作権メタデータを含む元ファイルを保持する。著作権者は公式 `Sans/README-third_party.md`、年は `Sans/HISTORY.md` と [GoogleのNoto Sans JP通知](https://github.com/google/fonts/blob/main/ofl/notosansjp/OFL.txt) でも照合した。

自動診断の合格マーカーは `PLAN7_BUNDLED_FONT_PASS`。文字の存在検査と画面確認は分け、720p／1080pで本文・ボタン・設定の実画面も確認する。
