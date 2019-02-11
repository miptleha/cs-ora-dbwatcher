--1. prepare table
create table tab1 (f1 int, f2 varchar2(100));
create table tab2 (f1 clob);

--2. made some changes (insert, update, delete)
begin
insert into tab1(f1, f2) values(10, 'hello');
insert into tab2(f1) values('long text');
commit;
DBMS_LOCK.sleep(1);
update tab1 set f1=15 where f2='hello';
commit;
DBMS_LOCK.sleep(1);
delete from tab1;
delete from tab2;
commit;
end;