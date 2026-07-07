using System.Runtime.CompilerServices;

// Test projesi internal repository/adaptörleri (ProducerReceiptRepository, ProducerTaxProfileReader/Writer)
// gerçek uygulamalarıyla test edebilsin diye görünür kılınır (Finance/Sales deseniyle birebir).
[assembly: InternalsVisibleTo("HalOS.Integration.Tests")]
