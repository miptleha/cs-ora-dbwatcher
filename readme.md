# Purpose
Console application that shows all changes in records for given set of tables.  
Useful if dont want analize program code and want just know what changes in db it performs.  
Can be applied not only for Oracle database but for all databases, where exist rowid analog.  

# Sample
* Create tables in db and fill them with data
```
create table tab1 (f1 int, f2 varchar2(100));
create table tab2 (f1 clob);

insert into tab1(f1, f2) values(10, 'hello');
insert into tab2(f1) values('long text');
commit;
```

* Write connection string and table names in app.config

```
<add name="DbConnection" connectionString="Data Source=(...);User ID=...;Password=..." />
<add key="Tables" value="tab1,tab2"/>
```

* Run program

Press Ctrl-F5 in Visual Studio 

* Edit data id db

```
delete from tab2;
insert into tab1(f1, f2) values(20, 'bay');
update tab1 set f1=15 where f2='hello';
commit;
```

* Watch program console or log\DbWatcher.log file

>tab1 (update): F1=15, F2=hello
>tab1 (before): F1=10, F2=hello
>tab1 (new): F1=20, F2=bay
>Deleted 1 rows from table tab2
