# Serverless Function Demo

This project is hosted on **Azure Functions (Serverless)**.

---

## 📬 Access via Postman

You can test the function endpoint directly in **Postman**:

- **URL:**  
  ```
  https://serverless-demo-b0h8dehugng5ddga.australiaeast-01.azurewebsites.net/api/StreamProducts
  ```

- **Required Headers:**
  | Key              | Value                                                               |
  |------------------|---------------------------------------------------------------------|
  | `x-functions-key`| `<value-of-the-secret>`          |

---

## 🚀 Example Workflow

1. Open **Postman**.  
2. Create a new **GET** request with the URL above.  
3. Add the required header:  
   - Key: `x-functions-key`  
   - Value: `<value-of-the-secret>`  
4. Send the request to fetch the stream of products.

---

✅ Now you can query the serverless function endpoint with Postman.
