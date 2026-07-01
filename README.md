# Mailing V2

WinForms 기반 입찰공고 메일링 시스템입니다.

## 주요 흐름

```text
Excel 업로드 -> Supabase DB 반영 -> 발송 대상 조회 -> SMTP 발송 또는 수동 발송완료 처리 -> 이력 관리
```

## 프로젝트

- `MailSender_v2`: 실제 개발 대상 WinForms 애플리케이션
- `tasks`: 작업 기준 문서와 진행 기록
- `AGENTS.md`, `CurrentWork.md`: Codex 작업 규칙과 현재 상태

`NaraEmailSender`, `NaraInfoCollectorBot` 등 기존 사본/참조 프로젝트와 실제 운영 데이터는 공개 리포지터리에 포함하지 않습니다.

## 실행 준비

1. Supabase SQL Editor에서 `MailSender_v2/Database/supabase_schema.sql`을 적용합니다.
2. 앱을 한 번 실행하여 빈 `config.json`을 생성합니다.
3. 생성된 `config.json`에 로컬에서만 다음 값을 입력합니다.

```json
{
  "Supabase": {
    "Url": "https://zogwawbwtxkogmgdvskb.supabase.co",
    "AnonKey": ""
  },
  "SmtpUser": "",
  "SmtpPw": "",
  "SmtpHost": "smtps.hiworks.com",
  "SmtpPort": 587,
  "SmtpEnableSsl": true,
  "Subject": "입찰공고 안내드립니다",
  "DefaultTo": "",
  "DefaultCc": "",
  "Body": [
    "안녕하세요.",
    "",
    "아래와 같이 입찰공고를 안내드립니다.",
    "감사합니다."
  ],
  "Images": [],
  "Download": [],
  "SendInterval": 5000,
  "MaxCount": 100
}
```

`config.json`은 Git에 커밋하지 않습니다. Supabase `service_role` key도 데스크톱 앱에서 사용하지 않습니다.

## 빌드

.NET Framework용 MSBuild 예시:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" "MailSender_v2\MailSender_v2.csproj" /p:Configuration=Debug /p:Platform=AnyCPU
```

NuGet 패키지는 `MailSender_v2/packages.config` 기준으로 복원합니다.

빌드 출력은 WinForms 실행 파일(`WinExe`)이며, 실행 시 별도 콘솔 창을 표시하지 않습니다. 실행 파일, 작업 표시줄, 앱 내부 보조 창 아이콘은 `MailSender_v2/nara-email.ico`를 기준으로 사용합니다.

## Supabase

초기 테스트 단계에서는 anon key로 앱 기본 기능이 동작하도록 RLS 정책을 완화합니다. 운영 전에는 사용자 인증, 제한 정책, 삭제/수정 권한 축소를 별도 task로 강화해야 합니다.

Supabase key 확인 위치:

1. Supabase Dashboard에서 프로젝트를 엽니다.
2. `Project Settings` 또는 `Settings`로 이동합니다.
3. `API Keys` 메뉴를 엽니다.
4. `Legacy API Keys`의 `anon` key 또는 `Project API keys`의 `publishable` key를 `Supabase.AnonKey`에 입력합니다.
5. `anon` key는 보통 `eyJ...`로 시작하는 JWT 형태이고, publishable key는 `sb_publishable_...` 형태입니다.
6. `service_role`, `secret`, `sb_secret_...` key는 데스크톱 앱에 입력하지 않습니다.

필수 테이블:

- `Recipients`
- `UploadHistory`
- `SendHistory`
- `BlockedEmails`

## 보안 주의

- SMTP 계정, 비밀번호, Supabase anon key는 코드/문서/로그에 기록하지 않습니다.
- 실제 Excel 메일링 리스트, 첨부파일, 발송 보고서는 Git에 올리지 않습니다.
- Public 리포지터리 기준으로 커밋 전 민감 문자열을 점검합니다.
