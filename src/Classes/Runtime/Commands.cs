using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Neuralia.Blockchains.Core.Logging;
using Neuralia.NClap.Metadata;
using Neuralium.Cli.Classes.API;

namespace Neuralium.Cli.Classes.Runtime.Commands
{



    
    public class CommandBase<API, API_METHODS> : Command
        where API : NeuraliumApi<API_METHODS>, new()
        where API_METHODS : IApiMethods 
    {
        public OptionsBase Arguments { get; set; }
        
        public API Api { get; set; }
        
    }
    public class OperationBase : CommandBase<NeuraliumApi<IApiMethods>, IApiMethods>
    {

        protected string operationName;

        protected List<string> parameters = new List<string>();

        protected double timeoutForLongOperation = 0.0;

        protected string jParameters;
        
        private IQueryJson PrepareQueryJson() {

            if(!string.IsNullOrWhiteSpace(this.Arguments.ConfigurationFile)) {
                if(!File.Exists(this.Arguments.ConfigurationFile)) {
                    throw new ApplicationException("Configuration file does not exist.");
                }

                string json = File.ReadAllText(this.Arguments.ConfigurationFile);

                return JsonSerializer.Deserialize<QueryJsonNamed>(json);
            }

            IQueryJson result = null;

            if(!string.IsNullOrWhiteSpace(this.jParameters)) {
                result = new QueryJsonNamed();
                ((QueryJsonNamed) result).Parameters = JsonSerializer.Deserialize<List<QueryJsonNamed.NamedOperationParameters>>(this.jParameters);
            } else if(this.parameters.Any()) {

                result = new QueryJsonIndexed();
                List<string> list = this.parameters.ToList();

                for(int i = 0; i < this.parameters.Count(); i++) {
                    ((QueryJsonIndexed) result).Parameters.Add(new QueryJsonIndexed.IndexedOperationParameters(i, list[i]));
                }
            } else {
                result = new QueryJsonIndexed();
            }

            if(!string.IsNullOrWhiteSpace(this.operationName)) {
                result.Operation = this.operationName;
            }

            return result;
        }
        
        public override Task<CommandResult> ExecuteAsync(CancellationToken cancel)
        {
            // TODO: Do something here.
            Console.WriteLine($"operation {operationName}, {this.parameters.Count} param(s): {String.Join(", ", this.parameters.ToArray())}");
            
            IQueryJson parameters = this.PrepareQueryJson();
            
            try {
                string result = this.Api.InvokeMethod(parameters, this.timeoutForLongOperation).ConfigureAwait(false).GetAwaiter().GetResult();

                if(!string.IsNullOrWhiteSpace(result)) 
                {
                    NLog.Default.Information(result);
                    Console.WriteLine(result);
                } else {
                    NLog.Default.Information("returned");
                }
            } catch(Exception ex) {
                NLog.Default.Error(ex, "Failed to query method.");
                return Task.FromResult(CommandResult.RuntimeFailure);
            }
            
            
            return Task.FromResult(CommandResult.Success);
        }
    }

    public class GenericOperation : OperationBase
    {
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The Operation Name")]
        public string OperationName
        {
            get => this.operationName;
            set => this.operationName = value;
        }

        [PositionalArgument(ArgumentFlags.Optional, Position = 1, ElementSeparators = new []{" "}, Description = "The sequential set of parameters.")]
        public List<string> Parameters
        {
            get => this.parameters;
            set => this.parameters = value;
        }
        
        [NamedArgument(ArgumentFlags.Optional, ShortName = "t", LongName = "timeout", DefaultValue = 0.0, Description  = "Wait 'timeout' seconds for completion in the case of long running operations. If timeout is reached, correlationId is returned.")]
        public double TimeoutForLongOperation
        {
            get => this.timeoutForLongOperation;
            set => this.timeoutForLongOperation = value;
        }
        
        [NamedArgument(ArgumentFlags.Optional, ShortName = "j", LongName = "jparams", DefaultValue = "", Description = "The json set of parameters. Usage: CreateNewWallet --jparams='[{\"AccountName\":\"account name\",\"AccountType\":1,\"EncryptWallet\":false,\"EncryptKey\":false,\"EncryptKeysIndividually\":false}'")]
        public string JParameters
        {
            get => this.jParameters;
            set => this.jParameters = value;
        }
        
        
    }

}