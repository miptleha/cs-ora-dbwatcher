# Purpose
Console application that shows all changes in records for given set of tables.  
Useful if you don't want to analize program code and want just know what changes in db was performed.  
Can be applied not only for Oracle database but for all databases, where exist rowid analog.  

# Sample
* Create tables in DB
```
create table tab1 (f1 int, f2 varchar2(100));
create table tab2 (f1 clob);
```

* Write connection string and table names in app.config
```
<add name="DbConnection" connectionString="Data Source=(...);User ID=...;Password=..." />
<add key="Tables" value="tab1,tab2"/>
```

* Run program   
Press Ctrl-F5 in Visual Studio 

* Edit data in DB
```
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
```

* Watch program console or log\DbWatcher.log file
> Scanning tables: tab1,tab2   
> Connection string: ...   
> tab1 (new): F1=10, F2=hello   
> tab2 (new): F1=long text   
> tab1 (update): F1=15 (10), F2=hello   
> tab1 (delete): F1=15, F2=hello   
> tab2 (delete): F1=long text    
