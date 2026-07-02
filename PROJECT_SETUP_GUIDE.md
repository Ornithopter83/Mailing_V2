# 프로젝트 세팅 가이드

GitHub: https://github.com/Ornithopter83/Mailing_V2

## 새 PC 또는 새 Codex 스레드에서 시작

1. 저장소를 clone한다.
2. 먼저 다음 문서를 읽는다.
   - `AGENTS.md`
   - `CurrentWork.md`
   - `tasks/MailSender_v2_revised.md`
   - 작업 대상 `tasks/*.md`
3. 작업 전 `git status`로 미커밋 변경을 확인한다.
4. 새 작업은 `tasks/00_TEMPLATE.md` 형식으로 task 문서를 만들거나, 기존 task 문서를 기준으로 진행한다.

## 로컬 실행 준비

1. Visual Studio 또는 MSBuild가 .NET Framework 4.8 빌드를 지원해야 한다.
2. NuGet 패키지는 `MailSender_v2/packages.config` 기준으로 복원한다.
3. Supabase SQL Editor에서 `MailSender_v2/Database/supabase_schema.sql`을 적용한다.
4. 앱을 한 번 실행하거나 `config.template.json`을 참고해 로컬 `config.json`을 만든다.
5. `config.json`에는 SMTP 계정, Supabase URL, anon/publishable key만 입력한다.

## 주의

- `config.json`, SMTP 비밀번호, Supabase key, 실제 Excel/첨부/보고서는 커밋하지 않는다.
- Supabase `service_role`, `secret`, `sb_secret_...` key는 데스크톱 앱에 입력하지 않는다.
- `MainForm.cs`는 코드 기반 UI이므로 Visual Studio Designer가 아니라 코드 편집기로 연다.

## 빌드 예시

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" "MailSender_v2\MailSender_v2.csproj" /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
```
