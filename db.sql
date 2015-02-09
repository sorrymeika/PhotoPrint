create table Users (
UserID int identity primary key,
Account varchar(200) unique,
UserName varchar(200),
Password varchar(32),
Avatars varchar(300),
Auth varchar(32),
RegisterDate datetime,
LatestLoginDate datetime
)

alter table Users add Gender bit
alter table Users add Birthday dateTime 
alter table Users add Mobile varchar(20)

create table PhotoAlbum (
AlbumID int identity primary key,
AlbumName varchar(200) not null,
Cover varchar(200),
UserID int not null,
Description varchar(4000),
CreationDate datetime,
EditDate datetime
)

create table Photo(
PhotoID int identity primary key,
AlbumID int,
Src varchar(200),
Description varchar(400),
CreationDate datetime,
EditDate datetime
)

create table Category(
CategoryID int identity primary key,
CategoryName varchar(40) unique,
Sort int
)

create table SubCate(
SubID int identity primary key,
CategoryID int not null,
SubName varchar(40),
Sort int
)

create table Product(
ProductID int identity primary key,
ProductName varchar(400),
ProductCode varchar(16),
Price decimal(18,2),
BaseInfo text,--������Ϣ
--[{name:"����",value:"����"},{name:"����",value:"����"},{name:"��װ",value:"���ϴ�"}]
Content text,
Tag varchar(200),
Sort int,
SubID int not null,
CreationDate datetime,
EditDate datetime,
Deleted bit
)

create table Color(
ColorID int identity primary key,
ColorName varchar(20),
ColorCode varchar(20),
ProductID int
)

create table Style(
StyleID int identity primary key,
StyleName varchar(200),
Rect varchar(200),--���Ƶ���������{top:0;left:0;width:0;height:0}
ProductID int
)

select * from Style

create table StyleColorPic(
PicID int identity primary key,
StyleID int,
ColorID int,
ProductID int,
Picture varchar(200)
)


create table ProductSize(
SizeID int identity primary key,
SizeName varchar(200),
StyleID int,
ProductID int
)

--�ٷ���Ʒ������Ʒ��
create table Work(
WorkID int identity primary key,
WorkCode varchar(30),
WorkName varchar(200),
WorkDesc varchar(2000),
ProductID int,
SoldNum int,
Picture varchar(200),
CreationTime datetime,
EditTime datetime,
Deleted bit
)

--�ٷ�������Ϣ
create table Customization(
CustomID int identity primary key,
WorkID int,
ProductID int,
StyleID int,
ColorID int,
SizeID int,
[Print] varchar(200),--�༭�õĴ�ӡͼƬ
Content text
--��������
--[{type:0,src:"ͼƬ��ַ",width:100,height:200,left:100,top:100,rotate:"��ת",...}
--,{type:1,text:"�ı�����",fontFamily:"����",...}]
)

select * from Customization

--������Ʒ
create table UserWork(
UserWorkID int identity primary key,
WorkID int,
UserID int,
ProductID int,
[Status] int,--����״̬ 0:������;1:���ﳵ��;2:�ѹ���;
Picture varchar(200),
CreationTime datetime,
EditTime datetime
)

--������Ʒ������Ϣ
create table UserCustomization(
CustomID int identity primary key,
UserWorkID int,
ProductID int,
StyleID int,
ColorID int,
[Print] varchar(200),--�༭�õĴ�ӡͼƬ
Content text
--��������
--[{type:0,src:"ͼƬ��ַ",width:100,height:200,left:100,top:100,rotate:"��ת",...}
--,{type:1,text:"�ı�����",fontFamily:"����",...}]
)

alter table UserCustomization add SizeID int

create table Cart(
CartID int identity primary key,
UserID int,
UserWorkID int,
Qty int,
AddTime datetime,
EditTime datetime
)

create table OrderInfo (
OrderID int identity primary key,
OrderCode varchar(30),
UserID int,
Status int,--����״̬��-3:���˿�;-2:�˿���;-1:��ȡ��;0:δ����;1:�Ѹ���;2:��ȷ��������;3�ѷ���;4:�������
CityID int,
RegionID int,
Receiver varchar(20),
Address varchar(400),
Mobile varchar(20),
Phone varchar(30),
Zip varchar(6),
Amount decimal(18,2),
Freight decimal(18,2),
AddTime datetime,
EditTime datetime
)

--alter table OrderInfo add Phone varchar(30)
alter table OrderInfo add PaymentID int
alter table OrderInfo add CouponID int

create table OrderDetail (
OrderDetailID  int identity primary key,
OrderID int,
UserWorkID int,
Qty int
)

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
RegionName varchar(50) not null,
CityID int not null,
Sort int
)

create table UserAddress(
AddressID int identity primary key,
UserID int not null,
Receiver varchar(30),
CityID int,
RegionID int,
Zip varchar(6),
Address varchar(600),
TelPhone varchar(30),
Mobile varchar(11),
IsCommonUse bit
)

create table ActivityCate (
CategoryID int identity primary key,
CategoryName varchar(200) unique,
Sort int
)
create table Activity (
ActivityID int identity primary key,
CategoryID int not null,
Title varchar(400),
Pic varchar(200),
Content text,
ActiveDate datetime,
CreationDate datetime,
EditDate datetime,
Deleted bit,
Sort int
)

create table CouponCate (
CategoryID int identity primary key,
CategoryName varchar(200) unique,
Sort int
)
create table Coupon(
CouponID int identity primary key,
Title varchar(200),
Memo varchar(8000),
Code varchar(16),
Number varchar(200),
CouponDate datetime,
CreationDate datetime,
EditDate datetime
)
alter table Coupon alter column Memo text
alter table Coupon add CategoryID int
alter table Coupon add CouponDateFrom datetime
alter table Coupon add Price decimal


create table CouponCode(
CodeID int identity primary key,
CouponID int,
Code varchar(32),
UserID int,
UseTimes int
)

alter table CouponCode add IsUsed bit

create table Admin(
AdminID int identity primary key,
AdminName varchar(100),
Password varchar(32)
)

insert into Admin (AdminName,Password) values ('admin','E10ADC3949BA59ABBE56E057F20F883E')


create table FeedBack(
FeedBackID int identity primary key,
UserID int,
Content varchar(500),
FeedBackTime datetime
)


--2014-06-04
alter table  OrderInfo add Discount decimal default 0
create table ProductComments (
CommentID int identity primary key,
ProductID int,
UserID int,
Content varchar(1000),
CommentTime datetime,
IsReview bit
)


--2014-09-04
--�����δִ��
alter table Users add RealName varchar(20)
alter table Users add Address varchar(500)
alter table Users add RegionID int

alter table Product add OrigPrice decimal(28,2)


--2014-09-29
create table Designer (
DesignerID int identity primary key,
DesignerName varchar(50),
Description varchar(100),
Avatars varchar(255)
)

create table Gallery (
GalleryID int identity primary key,
GalleryName varchar(100),
DesignerID int,
Picture varchar(255),
Votes int
)

alter table OrderInfo add [Message] varchar(1000) 


--2014-10-09
--������δִ��
select * from Activity
set IDENTITY_INSERT ActivityCate ON
if not exists(select 1 from ActivityCate where CategoryID=5) insert into ActivityCate (CategoryID,CategoryName) values (5,'��Ƹ��Ϣ')
if not exists(select 1 from ActivityCate where CategoryID=3) insert into ActivityCate (CategoryID,CategoryName) values (3,'��������')
if not exists(select 1 from ActivityCate where CategoryID=2) insert into ActivityCate (CategoryID,CategoryName) values (2,'���Ѷ')
set IDENTITY_INSERT ActivityCate OFF



alter table OrderInfo add Inv varchar(400)

--2015-02-04
alter table UserWork add Pictures varchar(2000)
alter table UserCustomization add Pictures varchar(2000)
