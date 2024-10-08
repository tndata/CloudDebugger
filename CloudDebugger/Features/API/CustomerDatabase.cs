﻿namespace CloudDebugger.Features.Api;

/// <summary>
/// CustomerDatabase
/// ================
/// 
/// This class contains a sample in-memory customer database
/// </summary>
public class CustomerDatabase
{
    private readonly List<Customer> m_customers;

    public CustomerDatabase()
    {
        m_customers =
        [
            new Customer { Id=1, FirstName = "Muriel", LastName = "Cook", StreetAddress = "22523 Willison Street", City = "Minneapolis", ZipCode = "55415"},
            new Customer { Id=2, FirstName = "Kenny", LastName = "Taylor", StreetAddress = "493 Sarah Drive", City = "Lake Charles", ZipCode = "70601" },
            new Customer { Id=3, FirstName = "Patrick", LastName = "Masi", StreetAddress = "932 White Lane", City = "Macon", ZipCode = "31201" },
            new Customer { Id=4, FirstName = "Stephanie", LastName = "Vidal", StreetAddress = "2265 Roosevelt Road", City = "Independence", ZipCode = "67301"},
            new Customer { Id=5, FirstName = "Angela", LastName = "DeCosta", StreetAddress = "751 Hamilton Drive", City = "Baltimore", ZipCode = "21223"},
            new Customer { Id=6, FirstName = "Carl", LastName = "Rutter", StreetAddress = "992 Stanley Avenue", City = "Lynbrook", ZipCode = "11563"},
            new Customer { Id=7, FirstName = "John", LastName = "Riley", StreetAddress = "3296 Smith Road", City = "Fairburn", ZipCode = "30213"},
            new Customer { Id=8, FirstName = "Kenneth", LastName = "Greiner", StreetAddress = "956 Jones Avenue", City = "Winston Salem", ZipCode = "27001"},
            new Customer { Id=9, FirstName = "Stephen", LastName = "McCormick", StreetAddress = "1177 Spadafore Drive", City = "Sigel", ZipCode = "15860"},
            new Customer { Id=10, FirstName = "James", LastName = "Hill", StreetAddress = "3759 Jefferson Street", City = "Atlanta", ZipCode = "30318"},
            new Customer { Id=11, FirstName = "Antonia", LastName = "Trevino", StreetAddress = "3031 Carson Street", City = "California", ZipCode = "02110"},
        ];

        //Add some more customers
        for (int i = m_customers.Count; i < 100; i++)
        {
            int zipcode = 25220 + i;
            var customer = new Customer { FirstName = "Joe-" + i, LastName = "Andersson", Id = i + 1, StreetAddress = "Main street #" + i, ZipCode = zipcode.ToString(), City = "Stockholm" };
            m_customers.Add(customer);
        }
    }


    /// <summary>
    /// Returns a list of all the customers
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Customer> GetAllCustomers()
    {
        return m_customers;
    }

    /// <summary>
    /// Get a specific customer
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Customer? GetCustomer(int id)
    {
        return m_customers.Find(x => x.Id == id);
    }

    /// <summary>
    /// Add a customer to the database
    /// </summary>
    /// <param name="customer"></param>
    /// <returns></returns>
    public int AddCustomer(Customer customer)
    {
        customer.Id = m_customers.Count + 1;
        m_customers.Add(customer);
        return m_customers.LastIndexOf(customer);
    }

    /// <summary>
    /// Remove a customer from the database
    /// </summary>
    /// <param name="id"></param>
    public void RemoveCustomerAt(int id)
    {
        m_customers.RemoveAt(id);
    }

    /// <summary>
    /// Update a specific customer
    /// </summary>
    /// <param name="id"></param>
    /// <param name="newData"></param>
    /// <returns>True if the customer was found and updated</returns>
    public bool UpdateCustomerAt(int id, Customer newData)
    {
        if (newData != null && id > 0)
        {
            var pos = m_customers.FindIndex(x => x.Id == id);

            if (pos >= 0)
            {
                m_customers[pos] = newData;
                return true;
            }
        }

        return false;
    }
}
