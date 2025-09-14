\connect reservations program

INSERT INTO hotel_availability (hotel_id, date, available_rooms)
SELECT h.id, d::date, h.rooms_count
FROM hotels h
CROSS JOIN generate_series(current_date, current_date + interval '3 years', interval '1 day') d;
