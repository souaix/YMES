# YMES 登入與密碼設計

## 使用者資料表

登入帳號只以 "YMES".tblusruserbasis 的 userno、username、password 為準；API 不從另一張表建立第二套帳號或密碼。來源字典中的 password 原為 varchar(50)，且沒有記載雜湊規則；migration 0002 將它擴成 varchar(255)，存放 ASP.NET Core Identity IdentityV3 格式的版本化 PBKDF2-HMAC-SHA512 雜湊（220,000 次）。新密碼先採 Unicode NFC 正規化，再進行雜湊。

`ym_auth_login_state` 是附加登入狀態，不存帳號密碼或角色：用外鍵連回人員主檔，只保存登入失敗次數、暫鎖時間及最近登入時間。登入憑證、啟用狀態與首次登入改密碼狀態只讀取 `tblusruserbasis`。來源字典沒有角色／權限對照表或 `USERLEVEL`、`USERTYPE` 值域，因此 API 不自行指定角色。

database/migrations/0001_authentication.sql 建立登入狀態與稽核事件表；0002_user_master_password_hash.sql 擴充原密碼欄位並建立人員主檔關聯；0003_password_hash_comment.sql 更新密碼欄位註解；0004_use_user_master_without_roles.sql 移除重複帳號與角色欄位。稽核內容不含密碼或雜湊。

## 密碼控制

- 登入密碼至少 4 個字元，最多 128 個字元；允許 `1234` 這類測試密碼，不強制大小寫、數字、符號組合。
- 使用 ASP.NET Core Identity IdentityV3 的版本化 PBKDF2-HMAC-SHA512 雜湊，每筆雜湊使用獨立隨機 salt，成本設為 220,000 iterations。資料庫只存雜湊，不存明文。
- 密碼長度依 Unicode code point 計算，並在設定或變更前檢查常見預設密碼與帳號關鍵字。
- `tblusruserbasis.resetpassword` 為 1 時，登入後需改密碼；一般密碼不定期強制輪替；帳號疑似外洩或使用者要求時立即更換。
- 每個來源 IP 的登入 API 以速率限制保護；帳號連續登入失敗達五次後暫鎖 15 分鐘。
- 使用 HttpOnly Cookie，生產環境強制 Secure；Web 前端不把密碼、Cookie 或登入 Token 存進 Local Storage。
- 正式、測試、開發環境使用各自的 API 部署和資料庫連線。Web 只選擇 API 服務位址，不直接連資料庫。

## 認證資訊保存

`tblusruserbasis.password` 只保存帶隨機 salt 的 PBKDF2 雜湊。登入時 API 以使用者輸入重新計算並驗證，不會把資料庫內容解密成原始密碼。此控制用來保護 ISO/IEC 27001 附錄 A.5.17 所管理的認證資訊。

## 執行設定

API 對外基底路徑為 `/YMES/API`；Nginx 將此路徑轉送給獨立 API 服務。API 透過部署環境或 Secret Manager 設定 ConnectionStrings__YMes、YMES_ALLOWED_ORIGINS、YMES_ENVIRONMENT、YMES_PATH_BASE、YMES_DATA_PROTECTION_PATH 與 AllowedHosts。正式環境的資料保護金鑰需持久化保存並限制檔案權限。不得將資料庫密碼、初始密碼或金鑰寫進版控。

Web 的 public/environments.json 只放 API 位址，不放資料庫帳密。尚未部署隔離的測試 API 時，測試區位址保持空白，避免帳密送進正式資料庫；正式區預設選中。
