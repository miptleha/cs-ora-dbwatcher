1. initial loading

create table tab1 (f1 int, f2 varchar2(100));
create table tab2 (f1 clob);

insert into tab1(f1, f2) values(10, 'hello');
insert into tab2(f1) values('long text');

2. changes after program starts

delete from tab2;
insert into tab1(f1, f2) values(20, 'bay');
update tab1 set f1=15 where f2='hello';
commit;