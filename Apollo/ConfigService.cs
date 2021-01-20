using Com.Ctrip.Framework.Apollo.Core;
using Com.Ctrip.Framework.Apollo.Core.Utils;
using Com.Ctrip.Framework.Apollo.Exceptions;
using Com.Ctrip.Framework.Apollo.Internals;
using Com.Ctrip.Framework.Apollo.Logging;
using Com.Ctrip.Framework.Apollo.Logging.Spi;
using Com.Ctrip.Framework.Apollo.VenusBuild;
using System;
using System.Configuration;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Web.Configuration;

namespace Com.Ctrip.Framework.Apollo
{
    /// <summary>
    /// Entry point for client config use
    /// </summary>
    public class ConfigService
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ConfigService));
        private static ConfigManager s_configManager;
        private static bool _isSave;
        private static bool _isWeb;
        static ConfigService() {
            try
            {
                ComponentsConfigurator.DefineComponents();
                s_configManager = ComponentLocator.Lookup<ConfigManager>();
            }
            catch (Exception ex)
            {
                ApolloConfigException exception = new ApolloConfigException("Init ConfigService failed", ex);
                logger.Error(exception);
                throw exception;
            }
        }

        /// <summary>
        /// 获取应用配置
        /// </summary>
        /// <param name="isSave">是否把配置保存到应用配置文件</param>
        /// <returns></returns>
        public static Config GetAppConfig(bool isSave=false)
        {
            _isSave = isSave;
            _isWeb = false;
            var config= GetConfig(ConfigConsts.NAMESPACE_APPLICATION);
            config.ConfigChanged += Config_ConfigChanged;
            AccessAppSettings(config, isSave);
            return config;
        }
        /// <summary>
        /// 获取web应用配置
        /// </summary>
        /// <param name="isWeb">当需要保存web.config时为true</param>
        /// <param name="isSave">是否把配置保存到应用配置文件</param>
        /// <returns></returns>
        public static Config GetWebConfig(bool isWeb=false,bool isSave = false)
        {
            _isSave = isSave;
            _isWeb = isWeb;
            var config = GetConfig(ConfigConsts.NAMESPACE_APPLICATION);
            config.ConfigChanged += Config_ConfigChanged;
            AccessAppSettings(config, isSave);
            return config;
        }
        private static void Config_ConfigChanged(object sender, Model.ConfigChangeEventArgs changeEvent)
        {
            foreach (string key in changeEvent.ChangedKeys)
            {
                Model.ConfigChange change = changeEvent.GetChange(key);
                AccessAppSetting(change.PropertyName, change.NewValue);
            }
        }

        private static void AccessAppSettings(Config config,bool isSave)
        {
            if (isSave)
            {
                //获取Configuration对象
                Configuration configuration =!_isWeb?System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None) :
                                              WebConfigurationManager.OpenWebConfiguration("~");
                foreach (var key in config.GetPropertyNames())
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Any(m => m == key))
                    {
                        configuration.AppSettings.Settings[key].Value = config.GetProperty(key, "");
                    }
                    else
                    {
                        //增加元素
                        configuration.AppSettings.Settings.Add(key, config.GetProperty(key, ""));
                    }
                }
                ////一定要记得保存，写不带参数的config.Save()也可以
                configuration.Save(ConfigurationSaveMode.Modified);
                ////刷新，否则程序读取的还是之前的值（可能已装入内存）
                System.Configuration.ConfigurationManager.RefreshSection("appSettings");
            }
            else
            {
                foreach (var key in config.GetPropertyNames())
                {
                    System.Configuration.ConfigurationManager.AppSettings.Set(key, config.GetProperty(key, ""));
                }
            }
            
            

        }



        private static void AccessAppSetting(string key, string value)
        {
            if (_isSave)
            {
                //获取Configuration对象
                Configuration configuration = !_isWeb
                    ? System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
                    : WebConfigurationManager.OpenWebConfiguration("~");
                if (ConfigurationManager.AppSettings.AllKeys.Any(m => m == key))
                {
                    configuration.AppSettings.Settings[key].Value = value;
                }
                else
                {
                    //增加元素
                    configuration.AppSettings.Settings.Add(key,value);
                }

                ////一定要记得保存，写不带参数的config.Save()也可以
                configuration.Save(ConfigurationSaveMode.Modified);
                ////刷新，否则程序读取的还是之前的值（可能已装入内存）
                System.Configuration.ConfigurationManager.RefreshSection("appSettings");
            }
            else
            {
                System.Configuration.ConfigurationManager.AppSettings.Set(key, value);
            }


        }

        /// <summary>
        /// Get the config instance for the namespace. </summary>
        /// <param name="namespaceName"> the namespace of the config </param>
        /// <returns> config instance </returns>
        public static Config GetConfig(String namespaceName) {
            return s_configManager.GetConfig(namespaceName);
        }
    }
}

