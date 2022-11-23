
using System.Xml.Linq;

namespace UK_BARCLAYCARD_SMARTPAY
{
    public class SmartPayOperations
    {

        public XDocument Payment(int amount, string transNum, string description, string sourceId, int currency, int country )
        {
            XDocument payment = XDocument.Parse(
                                  "<RLSOLVE_MSG version=\"5.0\">" +
                                  "<MESSAGE>" +
                                 "<SOURCE_ID>" + sourceId + "</SOURCE_ID>" +
                                  "<TRANS_NUM>" + transNum +
                                  "</TRANS_NUM>" +
                                  "</MESSAGE>" +
                                  "<POI_MSG type=\"submittal\">" +
                                   "<SUBMIT name=\"submitPayment\">" +
                                    "<TRANSACTION type= \"purchase\" action =\"auth_n_settle\" source =\"icc\" customer=\"present\">" +
                                    "<AMOUNT currency=\"" + currency + "\" country=\"" + country + "\">" +
                                      "<TOTAL>" + amount + "</TOTAL>" +
                                    "</AMOUNT>" +
                                    "<DESCRIPTION>" + description + "</DESCRIPTION>" +
                                    "</TRANSACTION>" +
                                   "</SUBMIT>" +
                                  "</POI_MSG>" +
                                "</RLSOLVE_MSG>");

            return payment;
        }

        public XDocument PaymentSettle(int amount, string transNum, string reference, string description, int currency, int country)
        {
            XDocument paymentSettle = XDocument.Parse(
                                  "<RLSOLVE_MSG version=\"5.0\">" +
                                  "<MESSAGE>" +
                                  "<TRANS_NUM>" + transNum +
                                  "</TRANS_NUM>" +
                                  "</MESSAGE>" +
                                  "<POI_MSG type=\"submittal\">" +
                                   "<SUBMIT name=\"submitPayment\">" +
                                    "<TRANSACTION type= \"purchase\" action =\"settle_transref\" source =\"icc\" customer=\"present\" reference= "
                                    + "\"" + reference + "\"" + "> " +
                                     "<AMOUNT currency=\"" + currency + "\" country=\"" + country + "\">" +
                                      "<TOTAL>" + amount + "</TOTAL>" +
                                    "</AMOUNT>" +
                                    "</TRANSACTION>" +
                                   "</SUBMIT>" +
                                  "</POI_MSG>" +
                                "</RLSOLVE_MSG>");

            return paymentSettle;
        }

        public XDocument ProcessTransaction(string transNum)
        {
            XDocument processTran = XDocument.Parse(
                              "<RLSOLVE_MSG version=\"5.0\">" +
                              "<MESSAGE>" +
                                "<TRANS_NUM>" +
                                    transNum +
                                "</TRANS_NUM>" +
                              "</MESSAGE>" +
                              "<POI_MSG type=\"transactional\">" +
                              "<TRANS name=\"processTransaction\"></TRANS>" +
                              "</POI_MSG>" +
                            "</RLSOLVE_MSG>");

            return processTran;

        }


        public XDocument PrintReciptResponse(string transNum)
        {
            XDocument printReceipt = XDocument.Parse(
                            "<RLSOLVE_MSG version=\"5.0\">" +
                            "<MESSAGE>" +
                              "<TRANS_NUM>" +
                                  transNum +
                              "</TRANS_NUM>" +
                            "</MESSAGE>" +
                            "<POI_MSG type=\"interaction\">" +
                              "<INTERACTION name=\"posPrintReceiptResponse\">" +
                                  "<RESPONSE>success</RESPONSE>" +
                              "</INTERACTION>" +
                            "</POI_MSG>" +
                          "</RLSOLVE_MSG>");

            return printReceipt;
        }


        public XDocument Finalise(string transNum)
        {
            XDocument finalise = XDocument.Parse(
                            "<RLSOLVE_MSG version=\"5.0\">" +
                            "<MESSAGE>" +
                              "<TRANS_NUM>" +
                                  transNum +
                              "</TRANS_NUM>" +
                            "</MESSAGE>" +
                            "<POI_MSG type=\"transactional\">" +
                             "<TRANS name=\"finalise\"></TRANS>" +
                            "</POI_MSG>" +
                          "</RLSOLVE_MSG>");


            return finalise;
        }

        public XDocument VoidTransaction(string transNum, string sourceId, string transRef)
        {
            XDocument cancel = XDocument.Parse(
                            "<RLSOLVE_MSG version=\"5.0\">" +
                            "<MESSAGE>" +
                                "<TRANS_NUM>" +
                                  transNum +
                              "</TRANS_NUM>" +
                            "<SOURCE_ID>" + sourceId + "</SOURCE_ID>" +
                            "</MESSAGE>" +
                            "<POI_MSG type=\"administrative\">" +
                             "<ADMIN name=\"voidTransaction\">" +
                              "<TRANSACTION reference =\"" + transRef + "\"></TRANSACTION>" +
                             "</ADMIN>" +
                            "</POI_MSG>" +
                          "</RLSOLVE_MSG>");

            return cancel;
        }


    }
}
