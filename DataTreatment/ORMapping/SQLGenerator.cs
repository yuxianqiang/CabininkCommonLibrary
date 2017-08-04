﻿using System;
using System.Linq;
using System.Text;
using Cabinink.TypeExtend;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cabinink.DataTreatment.Database;
namespace Cabinink.DataTreatment.ORMapping
{
   /// <summary>
   /// SQL语句生成实例，用于生成受CCL支持的数据库系统的SQL语句。
   /// </summary>
   [Serializable]
   [ComVisible(true)]
   public class SQLGenerator
   {
      private object _operatedObject;//需要生成对应成员SQL语句的对象。
      private string _sqlSentence;//SQL语句。
      private ESupportedDbSystem _supportedDbSystem;//CCL支持的DBS的名称枚举。
      /// <summary>
      /// 构造函数，创建一个指定操作对象SQL语句生成实例。
      /// </summary>
      /// <param name="operatedObject">需要生成对应成员SQL语句的对象。</param>
      /// <param name="supportedDbSystem">受CCL支持的DBS。</param>
      /// <exception cref="NotSupportedTypeException">当参数operatedObject指定的对象不受支持时，则会抛出这个异常。</exception>
      public SQLGenerator(object operatedObject, ESupportedDbSystem supportedDbSystem)
      {
         if (operatedObject.GetType().FullName == @"Cabinink.DataTreatment.ORMapping.SQLGenerator")
         {
            throw new NotSupportedTypeException();
         }
         _operatedObject = operatedObject;
         _sqlSentence = string.Empty;
         _supportedDbSystem = supportedDbSystem;
      }
      /// <summary>
      /// 获取或设置当前实例需要生成对应成员SQL语句的对象。
      /// </summary>
      public object OperatedObject
      {
         get => _operatedObject;
         set
         {
            if (value.GetType().FullName == @"Cabinink.DataTreatment.ORMapping.SQLGenerator")
            {
               throw new NotSupportedTypeException();
            }
            _operatedObject = value;
         }
      }
      /// <summary>
      /// 获取或设置当前实例生成的SQL语句。
      /// </summary>
      public string SqlSentence { get => _sqlSentence; set => _sqlSentence = value; }
      /// <summary>
      /// 获取或设置当前实例受支持的DBS。
      /// </summary>
      public ESupportedDbSystem SupportedDbSystem { get => _supportedDbSystem; set => _supportedDbSystem = value; }
      /// <summary>
      /// 获取当前被操作实例的属性信息集合。
      /// </summary>
      public List<(string, string)> PropertiesInfo
      {
         get
         {
            ObjectMemberGetter getter = new ObjectMemberGetter(OperatedObject);
            List<(string, string)> result = new List<(string, string)>();
            List<string> memberTypes = getter.GetPropertyTypes();
            List<string> memberNames = getter.GetPropertyNames();
            for (int i = 0; i < memberNames.Count; i++)
            {
               result.Add((new TypeMapping(0)
               {
                  SupportedDbSystem = SupportedDbSystem
               }.CTSMapping(memberTypes[i]), memberNames[i]));
            }
            return result;
         }
      }
      /// <summary>
      /// 创建一个指定名称，路径和日志记录文件路径的数据库，目前这个方法暂时支持SQLServer数据库的创建。
      /// </summary>
      /// <param name="databaseName">数据库的名称。</param>
      /// <param name="databaseFileUrl">数据库文件的物理地址。</param>
      /// <param name="logFileUrl">数据库日志文件的物理地址。</param>
      /// <exception cref="EmptySqlCommandLineException">如果用户选择SQLite和MSAccess数据库的SQL创建方式时，则需要抛出这个异常，原因是目前暂时不支持这两种数据库文件的SQL语句方式创建。</exception>
      public void GenerateSqlForCreateDatabase(string databaseName, string databaseFileUrl, string logFileUrl)
      {
         switch (SupportedDbSystem)
         {
            case ESupportedDbSystem.SQLServer:
               SqlSentence = @"create databse " + databaseName + " on(name=" + databaseName +
                  ",filename=" + databaseFileUrl + ",size=5MB,maxsize=20MB,filegrowth=20MB)log on(name=" + databaseName +
                  ",filename=" + logFileUrl + ",size=2MB,maxsize=10MB,filegrowth=1MB)";
               break;
            case ESupportedDbSystem.SQLite:
            case ESupportedDbSystem.MSAccess2003:
            default:
               throw new EmptySqlCommandLineException("目前暂时不支持SQLite和MSAccess数据库的SQL方式创建！");
         }
      }
      /// <summary>
      /// 创建一个数据表，数据类型系统由SupportedDbSystem属性指定。
      /// </summary>
      /// <param name="tableName">数据表的名称。</param>
      /// <param name="primaryKey">指定的主键字段，如果指定的字段在PropertiesInfo属性中无法被检索到，则会默认指定第一个字段为主键。</param>
      /// <param name="isNullField">一个列表集合，用于存储所对应字段是否允许为空字段，如果某个索引上的值为true，则表示这个这个索引所对应的字段允许为空，否则不允许为空。</param>
      public void GenerateSqlForCreateTable(string tableName, string primaryKey, List<bool> isNullField)
      {
         ExString baseSql = @"create table" + tableName + "(";
         List<(string, string)> propInfos = PropertiesInfo;
         if (isNullField.Count < propInfos.Count)
         {
            throw new OverflowException("isNullField参数的Coun属性不能小于当前实例的PropertiesInfo.Count属性！");
         }
         for (int i = 0; i < propInfos.Count; i++)
         {
            if (propInfos[i].Item1 == primaryKey) primaryKey = propInfos[i].Item1;
            else primaryKey = propInfos[0].Item1;
         }
         for (int i = 0; i < propInfos.Count; i++)
         {
            if (propInfos[i].Item1 == primaryKey)
            {
               baseSql = baseSql + propInfos[i].Item1 + " " + propInfos[i].Item2 + " primary key not null,";
            }
            else
            {
               string notNullString = isNullField[i] ? string.Empty : " not null,";
               baseSql = baseSql + propInfos[i].Item1 + " " + propInfos[i].Item2 + notNullString;
            }
         }
         SqlSentence = baseSql.SubString(0, baseSql.Length - 1) + ");";
      }
      public void GenerateSqlForDeleteTable(string tableName) => SqlSentence = @"drop table " + tableName;
   }
}
