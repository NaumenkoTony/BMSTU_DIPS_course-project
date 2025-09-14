\connect reservations program

CREATE TABLE hotels
(
    id          SERIAL       PRIMARY KEY,
    hotel_uid   uuid         NOT NULL UNIQUE,
    name        VARCHAR(255) NOT NULL,
    country     VARCHAR(80)  NOT NULL,
    city        VARCHAR(80)  NOT NULL,
    address     VARCHAR(255) NOT NULL,
    stars       INT,
    rooms_count INT          NOT NULL DEFAULT 3,
    price       INT          NOT NULL
);

CREATE TABLE reservation
(
    id              SERIAL      PRIMARY KEY,
    reservation_uid uuid        UNIQUE NOT NULL,
    username        VARCHAR(80) NOT NULL,
    payment_uid     uuid        NOT NULL,
    hotel_id        INT         REFERENCES hotels (id),
    status          VARCHAR(20) NOT NULL CHECK (status IN ('PAID', 'CANCELED')),
    start_date      TIMESTAMP WITH TIME ZONE,
    end_date        TIMESTAMP WITH TIME ZONE
);

CREATE TABLE hotel_availability
(
    id              SERIAL PRIMARY KEY,
    hotel_id        INT NOT NULL REFERENCES hotels (id),
    date            DATE NOT NULL,
    available_rooms INT NOT NULL,
    CONSTRAINT uq_hotel_date UNIQUE (hotel_id, date)
);
