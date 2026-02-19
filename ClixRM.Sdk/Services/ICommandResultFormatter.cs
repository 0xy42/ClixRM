using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClixRM.Sdk.Services;

public interface ICommandResultFormatter<TResult>
{
    void Format(TResult results);
}

