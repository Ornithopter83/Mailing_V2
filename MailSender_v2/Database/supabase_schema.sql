-- MailSender_v2 Supabase schema draft
-- Run this in the Supabase SQL Editor, then configure RLS policies for anon-key access.
-- Do not store a service_role key in the desktop application.

create table if not exists "Recipients" (
    "Id" bigserial primary key,
    "Email" text not null,
    "NormalizedEmail" text not null unique,
    "AgencyName" text,
    "NoticeDate" date,
    "NoticeName" text,
    "ManagerName" text,
    "Phone" text,
    "LastUploadedAt" timestamptz,
    "CreatedAt" timestamptz not null default now(),
    "UpdatedAt" timestamptz not null default now()
);

create table if not exists "UploadHistory" (
    "Id" bigserial primary key,
    "FileName" text,
    "FilePath" text,
    "SheetName" text,
    "TotalRows" integer not null default 0,
    "InsertedCount" integer not null default 0,
    "UpdatedCount" integer not null default 0,
    "EmptyEmailCount" integer not null default 0,
    "InvalidEmailCount" integer not null default 0,
    "DuplicateCount" integer not null default 0,
    "BlockedCount" integer not null default 0,
    "ErrorCount" integer not null default 0,
    "CreatedAt" timestamptz not null default now()
);

create table if not exists "SendHistory" (
    "Id" bigserial primary key,
    "RecipientId" bigint references "Recipients"("Id"),
    "Email" text not null,
    "Subject" text,
    "Status" text not null,
    "Method" text not null,
    "Memo" text,
    "ProcessedAt" timestamptz not null default now()
);

create table if not exists "BlockedEmails" (
    "Id" bigserial primary key,
    "NormalizedEmail" text not null unique,
    "Reason" text,
    "CreatedAt" timestamptz not null default now()
);

-- Initial test RLS policies.
-- These policies intentionally allow the desktop app to use the anon key during early testing.
-- Before production, replace them with authenticated/user-scoped policies and tighter write rules.

alter table "Recipients" enable row level security;
alter table "UploadHistory" enable row level security;
alter table "SendHistory" enable row level security;
alter table "BlockedEmails" enable row level security;

grant select, insert, update on table "Recipients" to anon;
grant select, insert on table "UploadHistory" to anon;
grant select, insert, delete on table "SendHistory" to anon;
grant select on table "BlockedEmails" to anon;

grant usage, select on sequence "Recipients_Id_seq" to anon;
grant usage, select on sequence "UploadHistory_Id_seq" to anon;
grant usage, select on sequence "SendHistory_Id_seq" to anon;
grant usage, select on sequence "BlockedEmails_Id_seq" to anon;

drop policy if exists "Recipients anon select" on "Recipients";
create policy "Recipients anon select"
on "Recipients"
for select
to anon
using (true);

drop policy if exists "Recipients anon insert" on "Recipients";
create policy "Recipients anon insert"
on "Recipients"
for insert
to anon
with check (true);

drop policy if exists "Recipients anon update" on "Recipients";
create policy "Recipients anon update"
on "Recipients"
for update
to anon
using (true)
with check (true);

drop policy if exists "UploadHistory anon select" on "UploadHistory";
create policy "UploadHistory anon select"
on "UploadHistory"
for select
to anon
using (true);

drop policy if exists "UploadHistory anon insert" on "UploadHistory";
create policy "UploadHistory anon insert"
on "UploadHistory"
for insert
to anon
with check (true);

drop policy if exists "SendHistory anon select" on "SendHistory";
create policy "SendHistory anon select"
on "SendHistory"
for select
to anon
using (true);

drop policy if exists "SendHistory anon insert" on "SendHistory";
create policy "SendHistory anon insert"
on "SendHistory"
for insert
to anon
with check (true);

drop policy if exists "SendHistory anon delete" on "SendHistory";
create policy "SendHistory anon delete"
on "SendHistory"
for delete
to anon
using (true);

drop policy if exists "BlockedEmails anon select" on "BlockedEmails";
create policy "BlockedEmails anon select"
on "BlockedEmails"
for select
to anon
using (true);
