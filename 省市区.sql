create table Province(
ProvID int primary key,
ProvName varchar(50),
Sort int,
Memo varchar(50)
)

create table City(
CityID int primary key,
CityName varchar(200) not null,
ProvID int not null,
Sort int
)


create table Region(
RegionID int primary key,
RegionName varchar(200) not null,
CityID int not null,
Sort int
)


insert into Province select PRV_ID,PRV_LOCALNAME,PRV_ORDER_NUM,PRV_MEMO from NET_ABS_CN.dbo.RPROVINCE
insert into City select CTY_ID,CTY_LOCALNAME,CTY_PRV_ID,CTY_ORDER_NUM from NET_ABS_CN.dbo.RCITY
insert into Region select REG_ID,REG_LOCALNAME,REG_CTY_ID,REG_ORDER_NUM from NET_ABS_CN.dbo.RREGION