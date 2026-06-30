# EMailService 현재 작업 상태

업데이트: 2026-06-29

## 프로젝트 개요

- 저장소: `C:\Projects\VS\EMailService`
- 기존 프로젝트:
  - `NaraEmailSender`: 콘솔형 메일 발송 프로젝트
  - `NaraInfoCollectorBot`: 나라장터 입찰공고 수집 프로젝트
- 신규 프로젝트:
  - `MailSender_v2`: WinForms 기반 입찰공고 메일링 시스템

## 현재 목표

기존 콘솔형 `NaraEmailSender`는 사본이므로 수정 가능하다. 다만 참조할 코드와 자료가 남아 있으므로, 먼저 `MailSender_v2` 작업용 사본을 만든 뒤 v2 구조로 개편한다.

초안 구현이 완성되고 SMTP 발송, HTML 본문, 이미지 CID, 첨부파일, 텍스트 보고서 등 필요한 참조가 모두 이전되면 기존 사본은 삭제해도 된다.

신규 프로젝트는 다음 운영 흐름을 목표로 한다.

```text
Excel 메일링 리스트 업로드
-> 이메일 정규화 및 유효성 검사
-> DB 저장 또는 기존 데이터 갱신
-> 발송 대상 조건 조회
-> 사용자가 체크박스로 대상 선택
-> 실제 SMTP 발송 또는 수동 발송완료 처리
-> 발송 이력 기록
-> 텍스트/Excel 결과 저장
```

## 기준 문서

- `AGENTS.md`
  - 작업 규칙
  - 변경 제한
  - task 문서 사용 기준
- `MailSender_v2_revised.md`
  - 신규 프로젝트 방향
  - UI 구성
  - DB 설계 초안
  - 수동 발송완료 처리
  - 권장 구현 순서
- `tasks/00_TEMPLATE.md`
  - 이후 작업 명령 추가 형식
  - task 단위 완료 조건과 결과 기록 형식

## 주요 결정 사항

### 작업용 프로젝트 생성

- `NaraEmailSender`를 작업용으로 복사하여 `MailSender_v2`를 만든다.
- 기존 프로젝트는 사본이므로 수정하거나 삭제할 수 있지만, 참조 이전 전에는 보존한다.
- 초안이 완성되고 필요한 참조가 모두 v2로 이전되면 기존 사본은 정리할 수 있다.

### 기존 프로젝트 참고 범위

`NaraEmailSender`에서 참고할 기능:

- SMTP 서버 연결
- 메일 제목, 본문, 참조, 숨은참조 처리
- HTML 본문 생성
- CID 기반 이미지 삽입
- 첨부파일 처리
- 발송 결과 텍스트 보고서 생성

### 신규 DB 중심 흐름

v2는 단순 Excel 즉시 발송이 아니라 Supabase DB 기반으로 동작한다.

권장 테이블:

- `Recipients`
- `UploadHistory`
- `SendHistory`
- `BlockedEmails`

### 수동 발송완료 처리

기존 방식으로 이미 보낸 메일이 있으므로, 실제 메일을 보내지 않고 조회 또는 선택된 대상을 발송완료 처리하는 기능이 필요하다.

처리 기준:

```text
Status = ManuallyMarkedSent
Method = Manual
Memo = 사용자가 발송 없이 발송완료 처리
ProcessedAt = 현재 시각
```

이 이력은 이후 발송 대상 조회에서 `이미 발송된 이메일 제외` 조건에 포함한다.

## 작업 흐름

### 0. 작업 준비

1. `AGENTS.md` 확인
2. `CurrentWork.md` 확인
3. `MailSender_v2_revised.md` 확인
4. 현재 작업에 해당하는 `tasks/*.md` 확인
5. 기존 `NaraEmailSender` 구조 확인
6. 변경 범위와 구현 순서 확인

### 1. 프로젝트 기반 생성

1. 기존 `NaraEmailSender`를 `MailSender_v2`로 복사
2. 솔루션, 프로젝트명, 네임스페이스, AssemblyName 정리
3. 콘솔 진입점을 WinForms 진입점으로 전환
4. 기본 폴더 구조 정리
5. 설정 파일 구조 초안 작성
6. 필요한 패키지 결정
7. 빈 메인 폼과 탭 구조 구성

### 2. DB 기반 구성

1. Supabase 연결 구성
2. Supabase 테이블 접근 확인
3. `Recipients` 테이블 생성
4. `UploadHistory` 테이블 생성
5. `SendHistory` 테이블 생성
6. `BlockedEmails` 테이블 생성

### 3. 목록 추가 탭

1. Excel 파일 선택 UI 구성
2. Excel 읽기 구현
3. 이메일 컬럼 탐색
4. 이메일 정규화
5. 파일 내부 중복 검사
6. 차단 목록 검사
7. DB 신규 등록 또는 기존 갱신
8. 업로드 상세 결과 DataGridView 표시
9. 최근 작업 로그 표시
10. 결과 텍스트 저장
11. 결과 Excel 저장

### 4. 발송 대상 조회

1. 공고일자 정렬 조건 구성
2. 발송 건수 조건 구성
3. 이미 발송된 이메일 제외 조건 구성
4. 차단 목록 제외 조건 구성
5. 대상 조회 쿼리 구현
6. DataGridView 체크박스 선택 구현
7. 선택 건수 표시

### 5. 수동 발송완료 처리

1. `발송완료 처리` 버튼 추가
2. 선택 대상 유효성 검사
3. MessageBox 확인
4. `SendHistory`에 `Manual` 이력 기록
5. 처리 로그 표시
6. 조회 결과 갱신

이 기능은 실제 SMTP 발송보다 먼저 구현할 수 있다.

### 6. 메일 작성 및 실제 발송

1. 제목 입력
2. 참조 입력 및 이메일 형식 검사
3. 본문 작성 영역 구성
4. 이미지 삽입
5. 첨부파일 추가 및 삭제
6. 임시저장
7. 미리보기
8. MessageBox 발송 확인
9. SMTP 발송
10. 성공/실패 이력 기록
11. 텍스트 보고서 생성

### 7. 로그 팝업

1. 공통 로그 팝업 구현
2. 업로드 로그 표시
3. 수동 발송완료 로그 표시
4. 실제 발송 로그 표시
5. 최근 50건과 전체 로그 구분

## 1차 개발 제외 항목

- 웹페이지
- 대시보드
- 발송 확인 전용 화면
- 발송 결과 전용 화면
- 통계 차트
- 예약 발송
- 사용자 권한 관리
- 복잡한 템플릿 관리
- 자동 재시도 관리 화면

## 검증 계획

구현 단계별로 가능한 검증을 수행한다.

- 프로젝트 생성 후 빌드 확인
- DB 초기화 확인
- 샘플 Excel 업로드 확인
- 업로드 결과 건수 확인
- 발송 대상 조회 조건 확인
- 수동 발송완료 처리 후 재조회 제외 확인
- SMTP 발송 전 유효성 검사 확인
- 발송 결과 보고서 생성 확인

## 분할 작업 계획

1. `tasks/01_프로젝트복사_기반정리.md`
   - 완료
   - 기존 `NaraEmailSender`를 `MailSender_v2`로 복사
   - 프로젝트명, 네임스페이스, WinForms 진입점, 기본 2탭 UI 초안 정리
2. `tasks/02_DB기반구성.md`
   - Supabase 기반 DB 연결 구성
   - `Recipients`, `UploadHistory`, `SendHistory`, `BlockedEmails` 테이블 구성
3. `tasks/03_목록추가_엑셀업로드.md`
   - Excel 파일 선택/읽기
   - 이메일 정규화, 중복/오류/제외 분류
   - DB 반영 및 업로드 결과 표시
4. `tasks/04_발송대상조회_수동완료.md`
   - 발송 대상 조회
   - 체크박스 선택
   - 실제 메일 발송 없는 수동 발송완료 처리
5. `tasks/05_메일작성_SMTP발송_로그.md`
   - 메일 작성
   - 첨부파일/미리보기
   - SMTP 발송
   - 발송 결과 기록 및 로그 팝업

## 다음 작업 후보

1. Supabase SQL Editor에서 `MailSender_v2/Database/supabase_schema.sql` 적용
2. `config.json`에 `Supabase.Url`, `Supabase.AnonKey` 설정
3. 앱 실행 후 4개 기본 테이블 접근 확인
4. 샘플 Excel 파일로 `목록 추가` 탭 업로드 확인
5. `tasks/04_발송대상조회_수동완료.md` 기준으로 발송 대상 조회와 수동 발송완료 처리 구현 시작

## 최근 작업 결과

### 2026-06-29 프로젝트 복사 및 기반 정리

- `NaraEmailSender`를 `MailSender_v2`로 복사했다.
- 솔루션/프로젝트명, 네임스페이스, 어셈블리명을 `MailSender_v2` 기준으로 정리했다.
- 콘솔 진입점을 WinForms 진입점으로 교체했다.
- 첨부 이미지와 `MailSender_v2_revised.md` 기준의 기본 2탭 UI 초안을 `MainForm.cs`에 구성했다.
- .NET Framework용 MSBuild로 빌드 성공을 확인했다.

### 2026-06-29 Supabase DB 기반 구성

- SQLite 기준 문서 누락을 Supabase 기준으로 수정했다.
- Supabase 접속 방식은 `HttpClient` 기반 PostgREST 직접 호출로 결정했다.
- `config.json`에서 `Supabase.Url`, `Supabase.AnonKey`를 읽는 설정 구조를 추가했다.
- 앱 시작 시 Supabase 기본 테이블 접근 확인을 수행하도록 연결했다.
- Supabase 설정이 비어 있으면 앱은 오류 없이 시작하고 설정 필요 메시지를 표시한다.
- Supabase SQL Editor용 스키마 초안 `MailSender_v2/Database/supabase_schema.sql`을 추가했다.
- 실제 Supabase 값이 없어 라이브 연결 검증은 수행하지 않았고, 빌드는 성공했다.

### 2026-06-29 목록 추가 Excel 업로드 구현

- `목록 추가` 탭의 파일 선택과 업로드 시작 버튼을 실제 처리 흐름에 연결했다.
- Excel에서 이메일 컬럼을 탐색하고, 이메일/기관명/공고일자/공고명/담당자/연락처를 가능한 범위에서 매핑하도록 했다.
- 이메일 정규화, 형식 오류, 빈 이메일, 파일 내부 중복, 차단 목록, 신규/기존 분류를 구현했다.
- Supabase 설정이 있으면 기존 이메일/차단 목록 조회 후 `Recipients` upsert, `UploadHistory` insert를 수행하도록 했다.
- Supabase 설정이 없으면 DB 반영 없이 파일 처리 결과만 화면에 표시하도록 했다.
- 업로드 상세 결과 DataGridView, 업로드 정보, 최근 작업 로그를 실제 처리 결과로 갱신하도록 했다.
- 결과 텍스트 저장과 결과 Excel 저장(`처리요약`)을 구현했다.
- 실제 Supabase 값이 없어 라이브 DB 반영 검증은 수행하지 않았고, 빌드는 성공했다.

### 2026-06-30 발송 대상 조회 및 수동 발송완료 구현

- 메일 탭의 발송 대상 조회 조건을 Supabase `Recipients`, `SendHistory`, `BlockedEmails` 기준 조회로 연결했다.
- 조회 결과 DataGridView 체크박스 선택, 전체 선택, 선택 해제, 선택 건수 표시를 구현했다.
- 선택 대상을 실제 메일 발송 없이 `SendHistory.Status = ManuallyMarkedSent`, `Method = Manual`로 기록하는 수동 발송완료 처리를 구현했다.
- 처리 후 재조회하여 `이미 발송된 이메일 제외` 조건에 반영되도록 했다.

### 2026-06-30 메일 작성, SMTP 발송, 로그 구현

- 제목/참조/본문/첨부파일 UI를 실제 입력값으로 사용하도록 연결했다.
- 임시저장, 미리보기, 첨부 추가/삭제, 이미지 참조 삽입을 구현했다.
- SMTP 발송 서비스를 분리하고 `config.json` SMTP 설정을 사용하도록 했다.
- 실제 발송 전 MessageBox 확인을 표시하며, 성공/실패 결과를 `SendHistory`와 텍스트 보고서에 남기도록 했다.
- 업로드, 수동 발송완료, SMTP 발송 로그를 공통 로그 팝업에서 볼 수 있게 했다.
- SMTP 실발송과 라이브 Supabase 기록 검증은 실제 설정값과 사용자 확인이 필요하여 수행하지 않았고, .NET Framework용 MSBuild 빌드는 성공했다.

### 2026-06-30 GitHub 및 Supabase 설정 준비

- `tasks/06_깃허브및Supabase설정.md`의 선택사항을 확인했다.
- GitHub 리포지터리는 Public 공개 기준으로 준비한다.
- `NaraEmailSender`와 `NaraInfoCollectorBot` 기존 참조 프로젝트는 공개 커밋 대상에서 제외한다.
- NuGet `packages/`, Visual Studio 산출물, 로컬 `config.json`, 인증서/키, 운영 Excel/첨부/보고서 파일을 `.gitignore`로 제외했다.
- `README.md`에 실행 준비, 로컬 설정 입력, 빌드 방법, Supabase SQL 적용, 보안 주의사항을 정리했다.
- `MailSender_v2/Database/supabase_schema.sql`에 초기 테스트용 RLS 정책과 anon role 권한을 추가했다.
- 루트 `.git` 폴더가 비어 있어 Git 저장소로 인식되지 않던 상태를 `git init`으로 초기화했다.
- origin을 `https://github.com/Ornithopter83/Mailing_V2.git`로 설정하고 `main` 브랜치에 초기 커밋을 push했다.
- Supabase SQL 실제 적용은 프로젝트 키/접속 권한이 필요하므로 로컬 SQL 파일 준비까지만 완료했다.

## task 문서 사용 방식

앞으로 명령이나 작업 추가는 `tasks/00_TEMPLATE.md` 형식을 따른다.

새 작업을 시작할 때는 다음 순서로 진행한다.

1. `tasks/00_TEMPLATE.md`를 기준으로 새 번호형 task 문서를 만든다.
2. 목표, 배경, 관련 파일, 변경 금지, 요구사항, 완료 조건, 테스트 방법을 채운다.
3. 구현 전 해당 task 문서를 기준으로 작업 범위를 확인한다.
4. 작업 완료 후 `결과` 섹션을 갱신한다.
5. 새로 확정된 큰 결정만 `CurrentWork.md`에 반영한다.
