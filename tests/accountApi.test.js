process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
const request = require('supertest');

const API_URL = 'https://localhost:5189'; 

describe('AccountApiController', () => {

const randomSuffix = Math.floor(Math.random() * 10000);

let testUser = {
  UserName: 'TestUser' + randomSuffix,
  FullName: 'Test User',
  Email: `testuser${randomSuffix}@example.com`,
  Password: 'Test@1234',
  PasswordConfirmation: 'Test@1234',
  PhoneNumber: '+380123456789'
};

  let jwtToken = '';

  test('POST /api/v1/accountapi/register - success', async () => {
    const res = await request(API_URL)
      .post('/api/v1/accountapi/register')
      .send(testUser);
      console.log(res.body);

    expect(res.statusCode).toBe(200);
    expect(res.body).toHaveProperty('message', 'User registered successfully');
  });

  test('POST /api/v1/accountapi/register - duplicate username/email', async () => {
    const res = await request(API_URL)
      .post('/api/v1/accountapi/register')
      .send(testUser);

    expect(res.statusCode).toBe(400);
    expect(res.body).toHaveProperty('UserName');
    expect(res.body).toHaveProperty('Email');
  });

  test('POST /api/v1/accountapi/login - success', async () => {
    const res = await request(API_URL)
      .post('/api/v1/accountapi/login')
      .send({
        UserName: testUser.UserName,
        Password: testUser.Password
      });

    expect(res.statusCode).toBe(200);
    expect(res.body).toHaveProperty('token');
    expect(res.body).toHaveProperty('message', 'Login successful');

    jwtToken = res.body.token;
  });

  test('POST /api/v1/accountapi/login - invalid password', async () => {
    const res = await request(API_URL)
      .post('/api/v1/accountapi/login')
      .send({
        UserName: testUser.UserName,
        Password: 'WrongPassword1!'
      });

    expect(res.statusCode).toBe(401);
    expect(res.body).toHaveProperty('message', 'Invalid username or password');
  });

  test('GET /api/v1/accountapi/profile - unauthorized', async () => {
    const res = await request(API_URL)
      .get('/api/v1/accountapi/profile');

    expect(res.statusCode).toBe(401);
  });

  test('GET /api/v1/accountapi/profile - authorized', async () => {
    const res = await request(API_URL)
      .get('/api/v1/accountapi/profile')
      .set('Authorization', `Bearer ${jwtToken}`);

    expect(res.statusCode).toBe(200);
    expect(res.body).toHaveProperty('userId');
    expect(res.body).toHaveProperty('username', testUser.UserName.toLowerCase());
    expect(res.body).toHaveProperty('email', testUser.Email.toLowerCase());
    expect(res.body).toHaveProperty('fullName', testUser.FullName);
    expect(res.body).toHaveProperty('phoneNumber', testUser.PhoneNumber);
  });

});