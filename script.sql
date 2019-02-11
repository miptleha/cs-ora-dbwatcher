--1. prepare table
create table tab1 (f1 int, f2 varchar2(100));

--2. made some changes (insert, update, delete)
insert into tab1(f1, f2) values(20, 'bay');
commit;
update tab1 set f1=15 where f2='hello';
commit;
delete from tab2;
delete from tab1;
commit;
