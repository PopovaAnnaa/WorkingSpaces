process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
const request = require('supertest');

const API_URL = 'https://localhost:5189';

function kyivTime(hour, minute) {
  const date = new Date(Date.UTC(2025, 10, 30, hour - 2, minute)); 
  return date.toISOString().replace('Z', '+02:00');
}

describe('Bookings API with PostgreSQL DB', () => {
  let jwtToken = '';
  let spaceId = 0;

  beforeAll(async () => {
    const loginRes = await request(API_URL)
      .post('/api/v1/accountapi/login')
      .send({ Username: 'ssherliann', Password: 'A~1234567890-' });

    expect(loginRes.status).toBe(200);
    expect(loginRes.body).toHaveProperty('token');
    jwtToken = loginRes.body.token;

    const spacesRes = await request(API_URL)
      .get('/api/spaces')
      .set('Authorization', `Bearer ${jwtToken}`);

    expect(spacesRes.status).toBe(200);
    expect(Array.isArray(spacesRes.body)).toBe(true);
    expect(spacesRes.body.length).toBeGreaterThan(0);

    spaceId = Number(spacesRes.body[0].id || spacesRes.body[0].spaceId);
  });

  it('should create a booking', async () => {
    const bookingData = {
      spaceId,
      startTime: kyivTime(10, 0),
      endTime: kyivTime(14, 0)
    };

    const res = await request(API_URL)
      .post('/api/bookings')
      .set('Authorization', `Bearer ${jwtToken}`)
      .send(bookingData);

    console.log(res.status, res.body);
    expect(res.status).toBe(201);
    expect(res.body.bookingId).toBeDefined();

    bookingId = res.body.bookingId; 
  });

  it('should list all bookings', async () => {
    const res = await request(API_URL)
      .get('/api/bookings')
      .set('Authorization', `Bearer ${jwtToken}`);

    expect(res.status).toBe(200);
    expect(Array.isArray(res.body)).toBe(true);
    expect(res.body.length).toBeGreaterThan(0);
  });

  it('should get my bookings', async () => {
    const res = await request(API_URL)
      .get('/api/bookings/my-bookings')
      .set('Authorization', `Bearer ${jwtToken}`);

    expect(res.status).toBe(200);
    expect(Array.isArray(res.body)).toBe(true);
  });

it('should update the booking', async () => {
    const updateData = {
      startTime: kyivTime(10, 0),
      endTime: kyivTime(15, 0)
    };

    const res = await request(API_URL)
      .patch(`/api/bookings/${bookingId}`)
      .set('Authorization', `Bearer ${jwtToken}`)
      .send(updateData);

    console.log(res.status, res.body); 
    expect(res.status).toBe(204); 
  });

it('should delete the booking', async () => {
    const res = await request(API_URL)
      .delete(`/api/bookings/${bookingId}`)
      .set('Authorization', `Bearer ${jwtToken}`);

    console.log(res.status, res.body);
    expect(res.status).toBe(204);
  });

  it('should not allow booking in the past', async () => {
    const pastBooking = {
      spaceId,
      startTime: "2023-01-01T09:00:00+02:00", 
      endTime: "2023-01-01T10:00:00+02:00"
    };

    const res = await request(API_URL)
      .post('/api/bookings')
      .set('Authorization', `Bearer ${jwtToken}`)
      .send(pastBooking);

    console.log(res.status, res.body);
    expect(res.status).toBe(400);
    expect(res.body.message).toMatch(/Invalid time/);
  });
});
