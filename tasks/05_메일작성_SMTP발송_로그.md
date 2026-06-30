# 작업명

메일작성_SMTP발송_로그

## 목표

* 메일 작성, 첨부파일, 미리보기, 실제 SMTP 발송, 발송 결과 기록, 로그 팝업을 구현한다.

## 배경

* 기존 `NaraEmailSender`에는 SMTP 발송, HTML 본문, CID 이미지, 첨부파일, 텍스트 보고서 생성 참고 코드가 있다.
* v2에서는 Supabase에서 조회/선택한 대상에게 메일을 발송하고 결과를 `SendHistory`에 남겨야 한다.

## 관련 파일

* `MailSender_v2/MainForm.cs`
* `MailSender_v2/Program.cs`
* `MailSender_v2/App.config`
* `MailSender_v2/bin/config.json`
* `NaraEmailSender/Program.cs`
* `MailSender_v2_revised.md`
* `tasks/메일작성.png`
* `tasks/04_발송대상조회_수동완료.md`

## 변경 금지

* SMTP 계정, 비밀번호, 토큰을 코드나 문서에 기록하지 않는다.
* 실제 발송 테스트는 사용자 확인 없이 수행하지 않는다.
* 기존 수동 발송완료 이력 의미를 변경하지 않는다.
* Supabase 스키마는 앞 단계 결과를 임의로 깨지 않는다.

## 요구사항

* 제목 입력값을 검증한다.
* 참조 이메일 형식을 검증한다.
* 본문 내용을 메일 본문으로 변환한다.
* 이미지 삽입 또는 기존 CID 이미지 방식을 연결한다.
* 첨부파일 추가/삭제를 구현한다.
* 첨부파일 경로 유효성을 검증한다.
* 임시저장 기능을 구현한다.
* 미리보기 기능을 구현한다.
* 발송 전 MessageBox 확인을 표시한다.
* 기존 SMTP 발송 방식을 서비스 구조로 이전한다.
* 발송 성공 시 `SendHistory.Status = Sent`, `Method = Smtp`로 기록한다.
* 발송 실패 시 `SendHistory.Status = Failed`와 실패 메시지를 기록한다.
* 발송 간격을 적용한다.
* 텍스트 보고서를 생성한다.
* 로그 팝업에서 업로드, 수동 발송완료, 실제 발송 로그를 확인할 수 있게 한다.

## 완료 조건

* 선택 대상에게 SMTP 발송 흐름을 실행할 수 있다.
* 발송 성공/실패 이력이 Supabase에 기록된다.
* 텍스트 보고서가 생성된다.
* 로그 팝업이 동작한다.
* 컴파일 경고/에러 없이 빌드된다.

## 테스트 방법

* .NET Framework용 MSBuild로 빌드
* SMTP 설정 누락 시 유효성 검사 확인
* 첨부파일 누락/잘못된 경로 검사 확인
* 사용자 승인 후 제한된 테스트 대상에게 발송
* `SendHistory`와 텍스트 보고서 확인

## 결과

* 제목/참조/본문 입력, 첨부파일 추가/삭제, 임시저장, 미리보기, 발송 버튼을 실제 이벤트로 연결했다.
* SMTP 발송 로직을 `Mailing/MailSendService.cs` 서비스로 분리했다.
* SMTP 설정은 `config.json`의 `SmtpHost`, `SmtpPort`, `SmtpEnableSsl`, `SmtpUser`, `SmtpPw`, `SendInterval` 값을 사용하도록 했다.
* 발송 전 MessageBox 확인을 표시하고, 사용자 확인 없이는 실제 SMTP 발송이 실행되지 않도록 했다.
* 발송 성공은 `SendHistory.Status = Sent`, 실패는 `SendHistory.Status = Failed`, 공통 `Method = Smtp`로 기록하도록 연결했다.
* 발송 결과 텍스트 보고서 `Report_yyMMdd-HHmm.txt`를 생성하도록 구현했다.
* 업로드/수동 발송완료/SMTP 발송 로그를 공통 로그 팝업에서 확인할 수 있게 했다.
* 이미지 삽입은 1차 구현에서 선택 이미지를 첨부 목록에 추가하고 본문에 참조 표시를 넣는 방식으로 연결했다. CID 본문 임베딩 고도화는 후속 개선 대상으로 남겼다.
* .NET Framework용 MSBuild Debug 빌드 성공을 확인했다.
