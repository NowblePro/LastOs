using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market
{
    public interface ISecurityStorage
    {
        /// <summary>
        /// take server type
        /// взять тип сервера. 
        /// </summary>
        /// <returns></returns>
        ServerType ServerType { get; }

        /// <summary>
        /// take securities
        /// взять инструменты
        /// </summary>
        List<Security> Securities { get; }

        /// <summary>
        /// take the security by the short name
        /// взять инструмент по короткому имени инструмента
        /// </summary>
        Security GetSecurityForName(string securityName, string securityClass);

        /// <summary>
        /// securities changed
        /// изменились инструменты
        /// </summary>
        event Action<List<Security>> SecuritiesChangeEvent;

    }
}
