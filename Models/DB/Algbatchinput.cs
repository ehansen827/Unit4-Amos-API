using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fjord1.Int.NetCore
{
    public partial class Algbatchinput
    {
        public string Account { get { return _account; } set { _account = value.Trim(); } }
        private string _account;
        public string Accountable { get { return _accountable; } set { _accountable = value.Trim(); } }
        private string _accountable;
        public string Address { get { return _address; } set { _address = value.Trim(); } }
        private string _address;
        public int Allocation_key { get; set; }
        public double Amount { get; set; }
        public int Amount_set { get; set; }
        public string Apar_id { get { return _apar_id; } set { _apar_id = value.Trim(); } }
        private string _apar_id;
        public string Apar_id_ref { get { return _apar_id_ref; } set { _apar_id_ref = value.Trim(); } }
        private string _apar_id_ref;
        public string Apar_name { get { return _apar_name; } set { _apar_name = value.Trim(); } }
        private string _apar_name;
        public string Art_descr { get { return string.IsNullOrEmpty(_art_descr) ? " " : _art_descr; } set { _art_descr = value.Trim(); } } 
        private string _art_descr;
        public string Article { get { return _article; } set { _article = value.Trim(); } }
        private string _article;
        public string Att_1_id { get { return _att_1_id; } set { _att_1_id = value.Trim(); } }
        private string _att_1_id;
        public string Att_2_id { get { return _att_2_id; } set { _att_2_id = value.Trim(); } }
        private string _att_2_id;
        public string Att_3_id { get { return _att_3_id; } set { _att_3_id = value.Trim(); } }
        private string _att_3_id;
        public string Att_4_id { get { return _att_4_id; } set { _att_4_id = value.Trim(); } }
        private string _att_4_id;
        public string Att_5_id { get { return _att_5_id; } set { _att_5_id = value.Trim(); } }
        private string _att_5_id;
        public string Att_6_id { get { return _att_6_id; } set { _att_6_id = value.Trim(); } }
        private string _att_6_id;
        public string Att_7_id { get { return _att_7_id; } set { _att_7_id = value.Trim(); } }
        private string _att_7_id;
        public string Bank_account { get { return _bank_account; } set { _bank_account = value.Trim(); } }
        private string _bank_account;
        public string Batch_id { get { return _batch_id; } set { _batch_id = value.Trim(); } }
        private string _batch_id;
        public string Clearing_code { get { return _clearing_code; } set { _clearing_code = value.Trim(); } }
        private string _clearing_code;
        public string Client { get { return _client; } set { _client = value.Trim(); } }
        private string _client;
        public string Client_ref { get { return _client_ref; } set { _client_ref = value.Trim(); } }
        private string _client_ref;
        public DateTime Confirm_date { get; set; }
        public string Contract_id { get { return _contract_id; } set { _contract_id = value.Trim(); } }
        private string _contract_id;
        public string Control { get { return _control; } set { _control = value.Trim(); } }
        private string _control;
        public double Cur_amount { get; set; }
        public string Currency { get { return _currency; } set { _currency = value.Trim(); } }
        private string _currency;
        public string Del_met_descr { get { return _del_met_descr; } set { _del_met_descr = value.Trim(); } }
        private string _del_met_descr;
        public string Del_term_descr { get { return _del_term_descr; } set { _del_term_descr = value.Trim(); } }
        private string _del_term_descr;
        public string Deliv_addr { get { return _deliv_addr; } set { _deliv_addr = value.Trim(); } }
        private string _deliv_addr;
        public string Deliv_attention { get { return _deliv_attention; } set { _deliv_attention = value.Trim(); } }
        private string _deliv_attention;
        public string Deliv_countr { get { return _deliv_countr; } set { _deliv_countr = value.Trim(); } }
        private string _deliv_countr;
        public DateTime Deliv_date { get; set; }
        public string Deliv_method { get { return _deliv_method; } set { _deliv_method = value.Trim(); } }
        private string _deliv_method;
        public string Deliv_terms { get { return _deliv_terms; } set { _deliv_terms = value.Trim(); } }
        private string _deliv_terms;
        public string Delivery_descr { get { return _delivery_descr; } set { _delivery_descr = value.Trim(); } }
        private string _delivery_descr;
        public string Dim_1 { get { return _dim_1; } set { _dim_1 = value.Trim(); } }
        private string _dim_1;
        public string Dim_2 { get { return _dim_2; } set { _dim_2 = value.Trim(); } }
        private string _dim_2;
        public string Dim_3 { get { return _dim_3; } set { _dim_3 = value.Trim(); } }
        private string _dim_3;
        public string Dim_4 { get { return _dim_4; } set { _dim_4 = value.Trim(); } }
        private string _dim_4;
        public string Dim_5 { get { return _dim_5; } set { _dim_5 = value.Trim(); } }
        private string _dim_5;
        public string Dim_6 { get { return _dim_6; } set { _dim_6 = value.Trim(); } }
        private string _dim_6;
        public string Dim_7 { get { return _dim_7; } set { _dim_7 = value.Trim(); } }
        private string _dim_7;
        public string Dim_value_1 { get { return _dim_value_1; } set { _dim_value_1 = value.Trim(); } }
        private string _dim_value_1;
        public string Dim_value_2 { get { return _dim_value_2; } set { _dim_value_2 = value.Trim(); } }
        private string _dim_value_2;
        public string Dim_value_3 { get { return _dim_value_3; } set { _dim_value_3 = value.Trim(); } }
        private string _dim_value_3;
        public string Dim_value_4 { get { return _dim_value_4; } set { _dim_value_4 = value.Trim(); } }
        private string _dim_value_4;
        public string Dim_value_5 { get { return _dim_value_5; } set { _dim_value_5 = value.Trim(); } }
        private string _dim_value_5;
        public string Dim_value_6 { get { return _dim_value_6; } set { _dim_value_6 = value.Trim(); } }
        private string _dim_value_6;
        public string Dim_value_7 { get { return _dim_value_7; } set { _dim_value_7 = value.Trim(); } }
        private string _dim_value_7;
        public double Disc_percent { get; set; }
        public double Discount { get; set; }
        public string Ean { get { return _ean; } set { _ean = value.Trim(); } }
        private string _ean;
        public double Exch_rate { get; set; }
        public string Ext_ord_ref { get { return _ext_ord_ref; } set { _ext_ord_ref = value.Trim(); } }
        private string _ext_ord_ref;
        public string Ext_order_id { get { return _ext_order_id; } set { _ext_order_id = value.Trim(); } }
        private string _ext_order_id;
        public string Intrule_id { get { return _intrule_id; } set { _intrule_id = value.Trim(); } }
        private string _intrule_id;
        public int Line_no { get; set; }
        public string Location { get { return _location; } set { _location = value.Trim(); } }
        private string _location;
        public string Long_info1 { get { return _long_info1; } set { _long_info1 = value.Trim(); } }
        private string _long_info1;
        public string Long_info2 { get { return _long_info2; } set { _long_info2 = value.Trim(); } }
        private string _long_info2;
        public string Lot { get { return _lot; } set { _lot = value.Trim(); } }
        private string _lot;
        public string Main_apar_id { get { return _main_apar_id; } set { _main_apar_id = value.Trim(); } }
        private string _main_apar_id;
        public string Mark_attention { get { return _mark_attention; } set { _mark_attention = value.Trim(); } }
        private string _mark_attention;
        public string Markings { get { return _markings; } set { _markings = value.Trim(); } }
        private string _markings;
        public DateTime Obs_date { get; set; }
        public DateTime Order_date { get; set; }
        public string Order_id { get; set; }
        public string Order_type { get { return _order_type; } set { _order_type = value.Trim(); } }
        private string _order_type;
        public string Pay_method { get { return _pay_method; } set { _pay_method = value.Trim(); } }
        private string _pay_method;
        public string Pay_temp_id { get { return _pay_temp_id; } set { _pay_temp_id = value.Trim(); } }
        private string _pay_temp_id;
        public int Period { get; set; }
        public string Place { get { return _place; } set { _place = value.Trim(); } }
        private string _place;
        public string Province { get { return _province; } set { _province = value.Trim(); } }
        private string _province;
        public string Rel_value { get { return _rel_value; } set { _rel_value = value.Trim(); } }
        private string _rel_value;
        public int Rent_flag { get; set; }
        public string Responsible { get { return _responsible; } set { _responsible = value.Trim(); } }
        private string _responsible;
        public string Responsible2 { get { return _responsible2; } set { _responsible2 = value.Trim(); } }
        private string _responsible2;
        public int Sequence_no { get; set; }
        public int Sequence_ref { get; set; }
        public string Serial_no { get { return _serial_no; } set { _serial_no = value.Trim(); } }
        private string _serial_no;
        public string Short_info { get { return _short_info; } set { _short_info = value.Trim(); } }
        private string _short_info;
        public string Status { get { return _status; } set { _status = value.Trim(); } }
        private string _status;
        public string Sup_article { get { return _sup_article; } set { _sup_article = value.Trim(); } }
        private string _sup_article;
        public string Swift { get { return _swift; } set { _swift = value.Trim(); } }
        private string _swift;
        public string Tax_code { get { return _tax_code; } set { _tax_code = value.Trim(); } }
        private string _tax_code;
        public string Tax_system { get { return _tax_system; } set { _tax_system = value.Trim(); } }
        private string _tax_system;
        public int Template_id { get; set; }
        public string Terms_descr { get { return _terms_descr; } set { _terms_descr = value.Trim(); } }
        private string _terms_descr;
        public string Terms_id { get { return _terms_id; } set { _terms_id = value.Trim(); } }
        private string _terms_id;
        public string Text1 { get { return _text1; } set { _text1 = value.Trim(); } }
        private string _text1;
        public string Text2 { get { return _text2; } set { _text2 = value.Trim(); } }
        private string _text2;
        public string Text3 { get { return _text3; } set { _text3 = value.Trim(); } }
        private string _text3;
        public string Text4 { get { return _text4; } set { _text4 = value.Trim(); } }
        private string _text4;
        public string Trans_type { get { return _trans_type; } set { _trans_type = value.Trim(); } }
        private string _trans_type;
        public string Unit_code { get { return _unit_code; } set { _unit_code = value.Trim(); } }
        private string _unit_code;
        public string Unit_descr { get { return _unit_descr; } set { _unit_descr = value.Trim(); } }
        private string _unit_descr;
        public double Unit_price { get; set; }
        public double Value_1 { get; set; }
        public string Vat_reg_no { get { return _vat_reg_no; } set { _vat_reg_no = value.Trim(); } }
        private string _vat_reg_no;
        public long Voucher_ref { get; set; }
        public string Voucher_type { get { return _voucher_type; } set { _voucher_type = value.Trim(); } }
        private string _voucher_type;
        public string Warehouse { get { return _warehouse; } set { _warehouse = value.Trim(); } }
        private string _warehouse;
        public string Wf_state { get { return _wf_state; } set { _wf_state = value.Trim(); } }
        private string _wf_state;
        public string Zip_code { get { return _zip_code; } set { _zip_code = value.Trim(); } }
        private string _zip_code;
        public DateTime Det_deliv_date { get; set; }
    }

}