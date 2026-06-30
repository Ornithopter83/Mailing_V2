# 작업명

깃허브 및 Supabase 설정

## 목표

* `EMailService` 작업 내용을 GitHub 리포지터리와 연결한다.
* Supabase 프로젝트에 `MailSender_v2` 운영에 필요한 DB 스키마와 접근 정책을 적용한다.
* SMTP 계정, Supabase 키, 실제 Excel/첨부/발송 결과 등 민감 데이터가 리포지터리에 등록되지 않도록 정리한다.
* 실제 구현 작업 전에 사용자가 선택해야 할 GitHub/Supabase 운영 방식을 문서화한다.

## 배경

* `MailSender_v2`는 `Excel 업로드 -> Supabase DB 반영 -> 발송 대상 조회 -> 실제 SMTP 발송 또는 수동 발송완료 처리 -> 이력 관리` 흐름으로 동작한다.
* 지금까지 로컬에서 01~05 task 구현을 진행했으므로, 이후 작업 전에 형상관리와 Supabase 설정 기준을 고정해야 한다.
* 기존 `NaraEmailSender` 사본, `bin/`, `obj/`, 실제 운영 Excel 파일, 첨부파일, `config.json`에는 공개 저장소에 올리면 안 되는 데이터가 포함될 수 있다.

## 관련 파일

* `AGENTS.md`
* `CurrentWork.md`
* `MailSender_v2_revised.md`
* `MailSender_v2/Database/supabase_schema.sql`
* `MailSender_v2/Config/AppSettings.cs`
* `MailSender_v2/bin/config.json`
* `.gitignore`
* `README.md`
* `tasks/01_프로젝트복사_기반정리.md`
* `tasks/02_DB기반구성.md`
* `tasks/03_목록추가_엑셀업로드.md`
* `tasks/04_발송대상조회_수동완료.md`
* `tasks/05_메일작성_SMTP발송_로그.md`

## 변경 금지

* SMTP 계정, 비밀번호, 토큰을 코드나 문서에 기록하지 않는다.
* Supabase `service_role` key는 데스크톱 앱, 코드, 문서, 로그, GitHub 리포지터리에 기록하지 않는다.
* 실제 발송 테스트는 사용자 확인 없이 수행하지 않는다.
* 기존 수동 발송완료 이력 의미를 변경하지 않는다.
* Supabase 스키마는 앞 단계 결과를 임의로 깨지 않는다.
* 실제 운영 Excel 파일, 첨부파일, 발송 결과 보고서, 로컬 `config.json`을 GitHub에 올리지 않는다.
* `bin/`, `obj/`, `.vs/`, 패키지 복원 산출물 등 빌드 결과물을 의도 없이 커밋하지 않는다.
* 기존 Git 이력이 이미 존재하는 경우 사용자 확인 없이 강제 push, rebase, reset, 기록 삭제를 하지 않는다.

## 요구사항

### 1. 사용자 확인이 필요한 선택사항

* GitHub 리포지터리: `Ornithopter83/Mailing_V2.git`
* Supabase project url: `https://zogwawbwtxkogmgdvskb.supabase.co`
* 리포지터리 공개 범위를 확인한다.
  * 선택 : Public 공개
* 기존 프로젝트 사본 포함 범위를 확인한다.
  * 선택 : 참조 이전 완료 후 `NaraEmailSender` 사본 삭제 후 커밋
* NuGet 패키지 폴더 처리 방식을 확인한다.
  * 선택 : `packages/` 제외 후 복원 기준으로 관리
* Supabase RLS 정책 수준을 확인한다.
  * 선택 : 초기 테스트를 위해 제한적으로 RLS 완화 후 운영 전 강화
* 로컬 설정 파일 처리 방식을 확인한다.
  * 선택 : 앱 최초 실행 시 빈 `config.json` 생성 로직만 유지

### 2. GitHub 정리 요구사항

* 작업 전 현재 Git 저장소 여부와 원격 URL을 확인한다.
* 원격이 없으면 `Ornithopter83/Mailing_V2.git`를 origin으로 설정할 수 있게 준비한다.
* `.gitignore`를 정리하여 다음 항목을 제외한다.
  * Visual Studio 산출물: `.vs/`, `bin/`, `obj/`
  * 사용자별 파일: `*.user`, `*.suo`
  * 실제 설정 파일: `config.json`, `*/bin/config.json`, `*/bin/Debug/config.json`, `*/bin/Release/config.json`
  * 실제 운영 데이터: Excel 파일, 발송 보고서, 첨부 PDF/이미지 중 운영 데이터
  * 인증서/키 파일: `*.pfx`, `*.p12`, `*.key`, `*.pem`
* 공개 가능한 설정 예시는 `config.example.json` 또는 README 예시로 제공하되 실제 키/비밀번호는 비워 둔다.
* 커밋 전 민감 문자열 후보를 점검한다.
  * `SmtpPw`
  * `AnonKey`
  * `service_role`
  * `apikey`
  * 실제 이메일 대량 목록
* README에는 다음 실행 준비 절차를 짧게 문서화한다.
  * Supabase SQL 적용
  * `config.json` 생성
  * `Supabase.Url`, `Supabase.AnonKey`, SMTP 설정 입력
  * .NET Framework용 MSBuild 빌드 방법

### 3. Supabase 설정 요구사항

* Supabase SQL Editor에 `MailSender_v2/Database/supabase_schema.sql`을 적용한다.
* 다음 테이블이 존재하고 앱에서 anon key로 접근 가능한지 확인한다.
  * `Recipients`
  * `UploadHistory`
  * `SendHistory`
  * `BlockedEmails`
* `Recipients.NormalizedEmail`과 `BlockedEmails.NormalizedEmail`의 중복 방지 조건을 확인한다.
* `SendHistory`에는 실제 SMTP 발송과 수동 발송완료 처리가 모두 기록되어야 한다.
  * SMTP 성공: `Status = Sent`, `Method = Smtp`
  * SMTP 실패: `Status = Failed`, `Method = Smtp`
  * 수동 발송완료: `Status = ManuallyMarkedSent`, `Method = Manual`
* 데스크톱 앱은 Supabase anon key만 사용한다.
* RLS 정책은 service role 없이 다음 앱 기능이 가능하도록 구성한다.
  * 대상자 조회
  * Excel 업로드 결과 upsert
  * 업로드 이력 insert
  * 발송 이력 insert
  * 차단 목록 조회
* 운영 전에는 anon key로 과도한 삭제/전체 수정이 가능하지 않은지 확인한다.
* DB 초기화 기능은 실제 적용 전 사용자 확인과 별도 task로 분리한다.

### 4. 로컬 설정 요구사항

* 실제 `config.json`에는 다음 값만 로컬에서 입력한다.
  * `Supabase.Url`
  * `Supabase.AnonKey`
  * `SmtpHost`
  * `SmtpPort`
  * `SmtpEnableSsl`
  * `SmtpUser`
  * `SmtpPw`
  * `SendInterval`
* `config.json`은 Git 추적 대상에서 제외한다.
* 예시 파일에는 민감값 대신 빈 문자열 또는 플레이스홀더만 둔다.
* 기존 코드의 설정 항목 이름과 문서의 예시 이름이 다르면 실제 코드 기준으로 맞춘다.

### 5. 작업 순서 요구사항

* 1단계: 저장소 상태와 제외 대상 확인
* 2단계: `.gitignore` 및 예시 설정 문서 정리
* 3단계: Supabase SQL/RLS 적용 준비 문서 정리
* 4단계: 사용자 선택사항 확인
* 5단계: 사용자 승인 후 GitHub 원격 연결, 커밋, push 진행
* 6단계: 사용자 승인 후 Supabase 설정 적용 또는 적용 안내

## 완료 조건

* 사용자가 선택해야 할 GitHub/Supabase 옵션이 문서에 정리되어 있다.
* GitHub에 올릴 파일과 제외할 파일 기준이 명확하다.
* 민감 정보가 Git에 포함되지 않도록 `.gitignore`와 예시 설정 기준이 문서화되어 있다.
* Supabase SQL 적용 대상과 RLS/anon key 원칙이 문서화되어 있다.
* 실제 GitHub push 또는 Supabase 변경은 사용자 확인 후 별도 작업으로 진행할 수 있다.
* 이 task 문서만으로 다음 작업자가 06 작업 범위와 금지사항을 이해할 수 있다.

## 테스트 방법

* 문서 검토
  * 요구사항에 GitHub 리포지터리와 Supabase URL이 포함되어 있는지 확인한다.
  * service role key 금지, 실제 `config.json` 제외, 운영 데이터 제외 기준이 있는지 확인한다.
* Git 적용 전 점검 예정
  * `git status`
  * `.gitignore` 적용 후 추적 대상 확인
  * 민감 문자열 검색
* Supabase 적용 전 점검 예정
  * SQL 스키마 내용 확인
  * anon key/RLS 정책 적용 범위 확인
  * 실제 키는 문서나 로그에 남기지 않고 앱 설정 파일에만 입력

## 결과

* `.gitignore`를 추가하여 Visual Studio 산출물, NuGet `packages/`, 로컬 `config.json`, 인증서/키, 운영 Excel/첨부/보고서, 기존 참조 프로젝트가 공개 리포지터리에 포함되지 않도록 정리했다.
* `README.md`를 추가하여 실행 준비, 로컬 설정 입력, 빌드 방법, Supabase 적용 파일, 보안 주의사항을 문서화했다.
* `MailSender_v2/Database/supabase_schema.sql`에 초기 테스트용 RLS 정책과 anon role 권한을 추가했다.
* `NaraEmailSender`는 `.gitignore`로 공개 커밋 대상에서 제외했다. 실제 폴더 삭제는 파괴적 작업이므로 별도 승인 또는 참조 이전 완료 재확인 후 진행한다.
* Git 저장소 상태 확인 결과 루트에 빈 `.git` 폴더가 있으나 Git 저장소로 인식되지 않았다. Git 초기화, origin 설정, 커밋, push는 사용자 승인 후 진행한다.
* Supabase SQL 실제 적용은 프로젝트 키/접속 권한이 필요하므로 로컬 SQL 파일 준비까지만 완료했다.
