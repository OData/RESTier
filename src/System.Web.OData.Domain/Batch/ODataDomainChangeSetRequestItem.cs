using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Batch;
using Microsoft.Data.Domain;
using Microsoft.Data.Domain.Submit;
using DomainEngine = Microsoft.Data.Domain.Domain;

namespace System.Web.OData.Domain.Batch
{
    public class ODataDomainChangeSetRequestItem : ChangeSetRequestItem
    {
        private DomainContext context;

        public ODataDomainChangeSetRequestItem(IEnumerable<HttpRequestMessage> requests, DomainContext context)
            : base(requests)
        {
            Ensure.NotNull(context, "context");

            this.context = context;
        }

        public override async Task<ODataBatchResponseItem> SendRequestAsync(HttpMessageInvoker invoker, CancellationToken cancellationToken)
        {
            Ensure.NotNull(invoker, "invoker");

            ODataDomainChangeSetProperty changeSetProperty = new ODataDomainChangeSetProperty(this);
            changeSetProperty.ChangeSet = new ChangeSet();
            this.SetChangeSetProperty(changeSetProperty);

            Dictionary<string, string> contentIdToLocationMapping = new Dictionary<string, string>();
            List<Task<HttpResponseMessage>> responseTasks = new List<Task<HttpResponseMessage>>();
            foreach (HttpRequestMessage request in Requests)
            {
                responseTasks.Add(SendMessageAsync(invoker, request, cancellationToken, contentIdToLocationMapping));
            }

            // the responseTasks will be complete after:
            // - the ChangeSet is submitted
            // - the responses are created and
            // - the controller actions have returned
            await Task.WhenAll(responseTasks);

            List<HttpResponseMessage> responses = new List<HttpResponseMessage>();
            try
            {
                foreach (Task<HttpResponseMessage> responseTask in responseTasks)
                {
                    HttpResponseMessage response = responseTask.Result;
                    if (response.IsSuccessStatusCode)
                    {
                        responses.Add(response);
                    }
                    else
                    {
                        DisposeResponses(responses);
                        responses.Clear();
                        responses.Add(response);
                        return new ChangeSetResponseItem(responses);
                    }
                }
            }
            catch
            {
                DisposeResponses(responses);
                throw;
            }

            return new ChangeSetResponseItem(responses);
        }

        internal async void SubmitChangeSet(ChangeSet changeSet, Action postSubmitAction)
        {
            SubmitResult submitResults = await DomainEngine.SubmitAsync(this.context, changeSet);

            postSubmitAction();
        }

        private void SetChangeSetProperty(ODataDomainChangeSetProperty changeSetProperty)
        {
            foreach (HttpRequestMessage request in this.Requests)
            {
                request.Properties.Add("Microsoft.Data.Domain.Submit.ChangeSet", changeSetProperty);
            }
        }

        private static void DisposeResponses(IEnumerable<HttpResponseMessage> responses)
        {
            foreach (HttpResponseMessage response in responses)
            {
                if (response != null)
                {
                    response.Dispose();
                }
            }
        }
    }
}
